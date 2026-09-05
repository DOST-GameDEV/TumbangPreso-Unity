# TODO: Tumbang Preso Unity

**The open worklist. If it is not open, it is in [`TODO_Archive.md`](TODO_Archive.md).**

Read [`../CLAUDE.md`](../CLAUDE.md) first and [`VISION.md`](VISION.md) second. Check this file
before inventing a task, and update it in the same commit as the work.

---

## CURRENT IMPLEMENTATION QUEUE

⚠️⚠️ **START HERE, AFTER `CLAUDE.md` AND `VISION.md`. THIS IS AN EXECUTION INDEX AND NOT A
REPLACEMENT FOR THE REASONING BELOW IT.** Each row is work a coding session can pick up and finish
on its own. **Use the detailed numbered sections further down for the evidence, the measurements
and the acceptance criteria**; they are why each row is worded the way it is, and a row summarised
away from its section loses the receipt that made it a task.

⚠️ **Do not resurrect archived work unless current code, a current test, or a row in this queue
gives a concrete reason.** [`TODO_Archive.md`](TODO_Archive.md) holds closed sections whole, with
their numbers, because the reasoning stays valuable. It is not a backlog.

⚠️ **Work that needs a person, a handset, an eye or a ruling is NOT here.** It is in
[`../Attention.md`](../Attention.md), which exists so this queue stays actionable. Adding a
human-only item here is how the queue stops being read.

⚠️⚠️ **AND WORK THAT SOMEBODY ELSE IS ACTIVELY HOLDING IS NOT HERE EITHER.** Controller support
(§ 142) has an owner and is being written on `controller-mapping`. **Do not implement, refactor or
"finish" any of it**, however open a row in § 138 looks: two people writing the same device layer
is worse than nobody writing it, and this session already deleted one competing generic-pad
implementation for exactly that reason.

**Nationals is the deadline this queue is ordered against.** P0 is "a match could be lost or a
build could ship wrong"; P1 is "this will cost an hour at the venue"; P2 is real work that is not
either of those.

| P | § | What is open | Done looks like |
|---|---|---|---|
| **OWNED** | 142, 138 | ⚠️⚠️ **CONTROLLER SUPPORT HAS AN OWNER AND IS NOT THIS QUEUE'S WORK. DO NOT PICK IT UP.** It is live on `controller-mapping`: `GenericPadBridge`, the CONTROLLER MAP screen, `MenuNav`, and a pad that can back out of a screen. | Nothing here. Read § 142 for what landed; leave the code to the person working on it. |
| **P1** | 145.6 | **A real multi-seed sweep has still not been RUN.** The harness is built and verified on synthetic arms; nothing has measured the shipped game across seeds since the numbers `CLAUDE.md` § 7.1 quotes | 5 to 8 Unity launches an arm, a `docs/reports/bot-sweep-*.md`, and the retrieval slide (§ 146) as the first thing compared against a pre-slide sweep. ⚠️ It is the only way to know whether § 146 changed the game |
| **P1** | 150.7 | ⚠️⚠️ **THERE IS NO AUDIO LISTENER AT THE PLAYER.** The game's only `AudioListener` sits on `~GameServices` at world origin and never moves, while every pooled voice is `spatialBlend = 1.0`. So distance and **pan are computed from the arena's centre rather than from the player's ears**: a cue 10 m in front of a player at `(-5,0,0)` pans right because it is world `+X`. Measured, with the arithmetic | Two halves, and the second is mandatory: the listener follows the active camera, **and** the seven non-diegetic cues fired at `Vector3.zero` get an explicit 2D path, or they break the same day. ⚠️ **Then an ear**, and what to judge is written down. § 150.7 |
| **P2** | 149.4 | **The one-shot / duplicate / replayable request sweep.** Three of its class are closed (§ 149.1, § 149.2, § 149.3). ⚠️ **The vote and reset paths were walked in § 150.9 and are idempotent by construction**; the rest of the list has not been | Concrete reachable bugs with focused tests, and no generalised framework. § 149.4, and § 150.9 for what has already been cleared |
| **P2** | 149.5 | **Repeated scene lookups in network hot paths.** Seat to `CharacterMotor`, seat to `Slipper`, the current `Lata` | ⚠️ **Measure first**: the exact APIs, the real call frequency, and whether the path is per packet or per frame. If it is negligible, record the number and leave the code alone. § 149.5. ⚠️ The two the row names first are **already caches, not scene searches**; the live surface is the AI and the combat sweep |
| **P2** | 149.7 | **The test suite's own value.** Source-text assertions that duplicate behavioural coverage, and a protocol floor checked twice | ⚠️ **For every test removed, name the stronger test that now owns that invariant.** Never delete a failing one to green the suite. § 149.7 |
| **P2** | 147.3 | **The game records its own good moments and nothing draws them.** Markers exist, are deterministic, are deduplicated and name a replay window | One reader. § 147.3 lists the three cheapest in order, and `CLAUDE.md` § 6.2's four questions come first |
| **ASTRA** | 146.6 | ⚠️⚠️ **THE RETRIEVAL SLIDE'S CLIP, THE HERO CASTS AND THE ULTIMATE CINEMATICS ARE ANIMATION WORK AND ARE NOT THIS QUEUE'S.** They are owned by Astra and queued in [`../ASTRA.md`](../ASTRA.md), one task per session. Same split as § 142 above | Nothing here. ⚠️ **The CODE side of § 146.6 is done**: both call sites ask for `"slide"` and the chain falls through to the lunge clip until a real one lands, so the clip drops in by name with no code change (§ 150.8). ⚠️ The FEEL of the numbers is `Attention.md` § 17.2 |
| **P2** | 127 | The taya ring and attacker disc need their non-colour distinction finished | § 127.3 |
| **HUMAN** | 144.3 | ⚠️ **MEASURED AND ANSWERED (§ 144.3b): it is a mirror, three generators were feeding it, and 61 of its 117 files differ from what ships.** It is no longer written to. **What is left is 🧑's**: it holds the pre-replacement sound for those 61 cues, and `CLAUDE.md` § 6 makes the sourced ones provisional until he has heard them | Deleting it is one commit once he says the sourced cues are keepers. Not autonomous work. `Attention.md` § 13 |
| **P2** | 144.8 | ⚠️ **MEASURED AND ANSWERED: it is causes 1 AND 2 together.** `JeepneyFinishProbe` had been in the repo since `e49bd2b` and never run. All 17 materials are `glTF/PbrMetallicRoughness` and `HasProperty("_Metallic")` is **False on every one**, so both writes were silently skipped; the scene also has **0 reflection probes**. Cause 3 is ruled out (every material carries `_finish`) | Write **`metallicFactor`** and **`roughnessFactor`**, the names the probe read off the shader. ⚠️⚠️ **Roughness is the INVERSE of smoothness**: 0.80 smoothness is 0.20 roughness, so do not transcribe the table. ⚠️ **And add a reflection probe**, or metal still renders flat. § 144.8 |

⚠️⚠️ **THE ROWS THAT NEED A PERSON ARE NOT HERE AND NEVER WERE.** 🧑's ear on the `ui_*` DC offset
(§ 144.3), his eye on the spectator fix and on the settings tabs, the reconnect-or-forfeit ruling
behind § 143.9, and the list of real pads behind § 138 are all in
[`../Attention.md`](../Attention.md). **Adding one back here is how this queue stops being read**,
which is the whole reason the split exists.

✅ **Closed by the 2026-09-04 hardening pass and no longer listed here: § 143.1, § 143.2, § 143.3, § 143.4, § 143.5, § 143.6, § 143.7, § 143.8, § 143.10, § 143.11, § 143.12, § 143.13, § 143.14, § 143.16, § 143.18.** Each one keeps its own subsection under § 143 with the measurement that closed it. **A done row is a row every future session reads and skips**, which is how an execution index turns back into the 22,930-line file this queue exists to replace.

✅ **Closed by the 2026-09-05 pass: § 134.12, § 143.9, § 143.15b, § 144.7, § 145.1, § 145.2, § 145.3, § 145.4.** Same rule: each keeps its subsection with the measurement, and its row is out of the queue above. ⚠️ **§ 145.5, § 146.6 and § 147.3 are the halves that stayed open**, and each says what it is waiting for.

✅ **Closed by the 2026-09-05 FOLLOW-UP pass: § 145.4b, § 145.7, § 145.8, § 145.9, § 145.10, § 145.11, § 145.12, § 145.13, § 145.14, § 149.1, § 149.2, § 149.3, § 149.9, § 149.11.** Four of those are competitive or release-integrity defects that were REACHABLE and are now closed with a regression each:

| § | What it was |
|---|---|
| **145.11** | the retrieval slide was a **free 1.75 m dash over the wire**: the host never asked whether there was anything to retrieve |
| **145.12** | and every refusal of it was **discarded before the rollback**, because the verb byte was bounded at `Shove` (2) and `Slide` is 3 |
| **149.1** | **packet frequency bought distance**. One second of server time was worth 70 m at 50 Hz and **4278 m at 5 kHz**, in a 14 m arena |
| **149.2** | the identity message **re-admitted a live connection under a token the client chose**, so a peer could take another's chair or move itself, and every repeat re-ran a room-wide broadcast fan-out |
| **149.9** | a spectator could set `Time.timeScale` to **NaN** on the host and every peer: `Mathf.Clamp` handles both infinities and passes NaN through |
| **149.11** | **every Unity run dirtied a tracked file**, so the qualification gate could never come out QUALIFIED however green the tests were |

### The reds that are about the game rather than the suite

⚠️⚠️ **THIS TABLE WAS WRITTEN FROM THE SINGLE-PROCESS RUN AND HALF OF IT WAS WRONG, WHICH IS
§ 143.1 DEMONSTRATED ON THIS FILE'S OWN CONTENTS.** Both `MatchRunTests` rows **pass in
isolation**: *"seat 1 never defended: the rotation is broken"* was another fixture's leaked world,
not a broken rotation, and a gate that reports the taya rotation broken for four runs running when
it is not is why nobody believed the suite. `PaperPurityProbe.NoFieldHighlightsInBlue` passes too.

**What actually survives isolation, on `64718d3`, after this pass fixed six of the twelve:**

| Case | What it says |
|---|---|
| ~~`CarryTests.AHeldSlipperStaysOnTheArm`~~ | ✅ **CLOSED, § 93 in the archive.** The 0.084 m was the deliberate `DrawnCentreOffset`, not slack: the test subtracted one of the carry's two terms. The bound is unchanged |
| `AspectRatioProbes.TheCharacterScreenSurvivesEveryAspectRatio` | 1 label under the 18-unit floor. ⚠️ **Do not lower the floor**; § 126.13 and `Attention.md` § 12.2 |
| `PaperPurityProbe.NothingOnTheInventoryDisappeared` | Controls that were on the screens before the paper pass and are not now |
| `CustomGameScreenProbe`, `PhaseSurfaceLayoutProbe`, `PlayerHubLayoutProbe` | Three labels wider than their boxes at 16:9 720p. `MenuKit.Label` overflows rather than wrapping, so each draws over its neighbour |
| `InputSurfaceProbe.EveryScreenHasAFocusPathAndReachableTouchTargets` | A press at the centre of Eskinita's `Button_NEXT MAP` lands on something else |

⚠️ **Do not fix a contamination failure by editing the code it happened to land on.** The way to
tell which is which is `python tools/playmode_suite.py --group <name>`, and it costs about two
minutes.

---

## What is open right now

Twenty-four sections, and this list is the whole of it. Everything else in this repository's history
is in the archive with its number unchanged.

⚠️ **§ 135 and § 136 CLOSED on 2026-09-04 and are in the archive.** § 137 is the pass that closed
them: the two-process harness § 135.7 said did not exist, the bad-wifi table and disconnect matrix
it was blocking, the three UNGATED cue rows of § 135.6, and § 136.4's touch control. **What § 135.7
listed as needing a HUMAN is still open and is in [`../Attention.md`](../Attention.md)**, not here:
Android thermals need a handset, and a phone joining a PC needs a person to watch it.

| § | Open work | Where it bites |
|---|---|---|
| **150** | The camera/feel/lifecycle pass: a hitstop that drifted 11.9 m, a bearing nobody passed, and an audit blind to its own worst case | ⚠️⚠️ **Three confirmed defects, all closed with a regression each**: the hit freeze accumulated its punch every held frame (**11.890 m measured** against a 0.45 m ceiling, and it got WORSE at a higher frame rate); every hero skill hit punched the camera along `-victim.forward` because five call sites dropped `HitFeel.Land`'s optional `from`; and `audit_event_subscriptions.py` could see neither anonymous delegates nor Unity's camelCase events, which are the two shapes most likely to leak. § 149.8's two-match run is built. ⚠️ **§ 150.7 is the open one and it needs an ear.** ⚠️ The animation half of that brief is `ASTRA.md`, not here |
| **149** | The fresh-audit follow-up: movement budget, re-admission, the one-shot requests | ⚠️⚠️ **Six confirmed defects, five of them competitive or release-integrity, all closed with a regression each.** § 149.4 to § 149.8 are what is left, and § 149 carries the brief as well as the record |
| **147** | The game records its own good moments and nothing draws them | Markers exist, are deduplicated, are deterministic and name a replay window. **No screen reads them.** § 147.3 lists the three cheapest readers, in order |
| **146** | The retrieval slide is built, derived and tested; nobody has FELT it | 🧑's own test is *"I can safely approach and pick this up normally, OR I can commit"*. Nobody uses it means the recovery is too long; normal retrieval stopping means it is too cheap. Both are one constant and both are `Attention.md` § 17.2 |
| **145** | Four gates that could come out green while proving less than they printed | Three closed. ⚠️ **§ 145.5 is a machine fact**: a nationals certification comes off the Windows laptop, because this Mac has no Windows Standalone module and no dotnet |
| **142** | Controller support: a picture of the pad you can rebind from, a pad that can leave a screen, and an unrecognised pad that works | 🧑, with a labelled DualShock drawing: *"we will now be implementing controller support. create a menu for controller mapping and ensure controller support works in the game."* ⚠️⚠️ **The map is built and so are the three faults nobody had looked for: EVERY back-out in the game was a keyboard-only `Input.GetKeyDown(KeyCode.Escape)`, so a pad could reach every screen and leave none of them; there was no pause on a pad at all; and the emote wheel opened on the d-pad and could not be steered by one.** § 138 steps 2 and 3 are closed by the same pass. Open: 🧑's eye on it, and a written list of real pads. § 142 |
| **141** | Spectator and a driven seat were on screen at the same time, and F1-F4 have two readers | 🧑, with a screenshot: **“IF IT isnt spectator why do i see spectator hud”**. ✅ **Cause found and fixed: `Hud.EnterSpectatorMode` had no inverse**, so every re-seat after a spectator window left the HUD stripped and the overlay drawn. The F1-F4 double-read is fixed too, and § 141.2 breaks the premise `CLAUDE.md` § 4 exempts the nine spectator keys on. ✅ **The duplicate name is closed by § 141.8**: `Core.BoardNames` names the seat when two share one, and `MatchInvariants.CheckSeatClaims` is still what reports one person driving two chairs. ⚠️ **Open: only 🧑's eye on the fix, which is in `Attention.md`.** § 141 |
| **140** | The player cannot see the network, and the timeout gives them eight blind seconds | ⚠️⚠️ **The biggest open network item, and it was found by measuring.** There is no ping, no bars, no "reconnecting" anywhere in the game, and `DisconnectTimeoutMS` is 8000, so a peer whose wifi dies keeps a normal-looking arena for eight seconds. **The sampler is built (§ 140.3); the screen is designed and not built (§ 140.4).** § 140.5 is the one that needs a decision rather than code. § 140 |
| **139** | Settings is four pages now, and the renders found three faults older than the pass | 🧑: *"we have too many settings now"*, *"add tabs or some shit so that they dont have to scroll that much"*. **Done and rendered; it is open because he has not looked at it.** The renders also found blue slider fills and a magenta tick that had shipped the whole port below the fold. § 139 |
| **138** | A controller Unity does not recognise is invisible to this whole game | 🧑: *"idk how extensive controller support is"*, *"maybe add to todo that it can work for fake controllers and shit too"*. Every controller path reads `Gamepad.current` or a `<Gamepad>/` path, and an unmatched pad is a `Joystick` that none of them see. ✅ **Steps 1, 2 and 3 are DONE.** It warns (`ControllerWatch`), it is DRIVEN through a guessed mapping (`GenericPadBridge`), and the guess is visible and rebindable (`ControllerMapScreen`). ⚠️ Open: **step 4, a written list of pads anybody has really tested**, which needs hardware rather than code. § 138, § 142 |
| **134** | The broadcast pass: autopilot, replay, ultimate introductions, the shove that meant nothing, and the keyboard on the phone | 🧑: *"why the fuck does it have keybinds theres no keys in mobile"*, and bots that *"follow players around only to push them"*. **The touch layer, the AI shove, the autopilot, the replay, the six ultimate introductions and Eskinita are done and captured; § 134.10, § 134.12, § 134.15 and § 134.16 are what is left open.** ⚠️ § 134.9 is CLOSED by § 137. § 134 |
| **133** | One font is doing every job, and it is a display face | 🧑: *"I think the problem is we use the same font for everything"*. **The next session's brief**: a body face that pairs with Darumadrop, plus the lobby and login overhaul, with a logo he is attaching. § 133 |
| **132** | The loadout said nothing about the hero, and a build vanished the moment the match started | Twelve defaults read `As tuned · As tuned`, the ultimate was not on the board, the hold-key panel named the SLOT rather than the equipped reading, and the TAB tray printed every ability name twice. § 132 |
| **131** | Replace Hero Strike's primitive VFX and the synthesised SFX from the verified source list | **Five of the six families are wired; 24 sourced cues remain and 3 preferred old cues were restored (§ 131.3, § 131.5b).** Open: the other twelve abilities, Phaister's draped plate, and the two downloads behind a login |
| **130** | Crossplay, the boot ANR and the lobby's missing ink | The architecture was fine; **two phone-side defects and a third camera built without all three passes**. ⚠️ § 130.14 is CLOSED (§ 130.14b): the steering was never wrong, the test buried its own seat in the floor |
| **126** | The full PlayMode suite, the thumb floor, the move stick, rumble, the device toggle, the .apk | § 126.8 is still the big one and it is **narrower now**: § 126.8b has the cause (the reset reached five fixtures out of sixty), § 126.8c is a `.xml` that says `Passed` with `total="0"`, § 126.8d is a fix that was measured and **withdrawn** for moving eleven suites from one side to the other |
| **127** | Phase 16.1: the taya's floor marker is a RING, an attacker's a DISC | Needs its greyscale frame before it can close. § 127.3 |
| **128** | Phases 11 and 12 are almost entirely built | ⚠️ Phase 11 has **nothing** open. **Map rotation is built (§ 130.12)**; Phase 12 still owes LAST TSINELAS a match half, and § 130.13 is what that actually costs |
| **126.11** | Crossplay is argued, not demonstrated | Both players exist; nobody has watched them join. ⚠️ **The two blockers it named are fixed in § 130.2 and § 130.3** |
| **96** | He has never found the way into the hub | The door, not the layout. `CLAUDE.md` § 6.3 |
| **95b** | Nothing asserts that a menu label FITS, only that it is legible | Two probes, neither asking the question |
| **72** | Two lobby controls reported dead that every headless check says are alive | Reported by a person, green in every test |
| **68** | The lobby is a form and it should be a room | Planned, not built |
| **69** | No chat, in the lobby or in a match | Planned, not built |
| **88, 89** | Accounts and the career layer, in progress | Read § 89.6 before touching `ProfileRules`, written in C# and again in JS |
| **118, 119, 121** | The paper front end: what is coherent and what is not finished | |

⚠️⚠️ **AND THE "UNSTARTED" PHASES ARE NOT ALL UNSTARTED, WHICH IS WHY § 0.6 EXISTS.** Bots and
population (11), modes and maps (12), seasons (13), accessibility (16), tournaments and replays
(17) and getting it in front of people (18) live in [`FUTURE.md`](FUTURE.md) with a written prompt
each. **They are things somebody might decide to do; an entry here is something somebody should
do.** Checked against the code on 2026-09-03, before doing any of them:

| Phase | What is actually there |
|---|---|
| **11** | ⚠️ **Mostly built.** Difficulty tiers, a NONE option, `BOT` labels and `MatchRecord.IsBot` all ship. § 128 is the open half: **the rating does not read the flag** |
| **12** | ⚠️ **Both modes exist in the core.** `MatchFormat.LastTsinelas` and `Mirror`, with `CustomGame.MirrorIndex` doing the weekly rotation. LAST TSINELAS still has **no match half**. **Map rotation and a map vote are the genuinely unbuilt cheap win** and nothing greps for either |
| **16** | ⚠️ **Started.** § 127: the taya's floor marker is a ring. The crosshair and the lata label are still hue-only, and none of § 16.2 is begun |
| **13, 17, 18** | Not started. ⚠️ § 17's unplugged LAN run is DONE and must not be re-raised |

---

## The five things worth knowing before you touch anything

⚠️⚠️ **DO NOT READ `NetSession.ProtocolVersion` OFF THIS PARAGRAPH. READ IT OFF THE FILE.**

```bash
grep -n "public const int ProtocolVersion" Assets/TumbangPreso/Runtime/Net/NetSession.cs
```

⚠️⚠️ **THIS LINE HELD A NUMBER FOR ITS WHOLE LIFE AND THE NUMBER WAS WRONG FIVE TIMES.** It said
22 while the code said 23; before that it carried **both 19 and 21** as "the" number for two days,
four paragraphs apart, because each session appended its own line and nobody deleted the last one;
and it said **21** for a day after § 130.13 moved it to 22. **It said 23 until the 2026-09-05 pass
moved it to 24** (the seat handover's rating, § 144.7), which is the fifth time. Peers on different
numbers refuse each other by design, so a stale number here sends somebody hunting a network bug
that is a rebuild.

**So it names no number at all now.** The command above is two seconds and cannot be wrong, and
the two tests that own the exact value, `InputContractTests
.TheInputPassDidNotMoveTheProtocolVersion` and `ChatAndLobbyChromeTests
.TheProtocolCarriesEveryRosterBump`, go red when it moves, which is what makes a bump a
deliberate act rather than something to notice later.

⚠️ **`docs/FUTURE.md` § 0.2's row said 16 for four days** while the code went 17 through 23. **The
number lives in one file and every copy of it is a liability**, which is why both places now say
so rather than saying a number.

⚠️⚠️ **A GREEN LAYOUT PROBE IS NOT A GOOD SCREEN, AND A GREEN FULL PLAYMODE RUN IS NOT A GATE.**
The first is `CLAUDE.md` § 6.2a; the second is § 126.8 and it is new. Verify with `-testFilter`
over the suites you touched.

⚠️ **Every door in the front end gets read before it gets moved.** § 96 and § 115.6 are the same
fault twice: he commissioned a feature, read the entry that shipped it, and could not find it.

⚠️ **The unplugged LAN run is DONE** (2026-08-31) and must not be re-raised as outstanding. The
requirement it protects stands: a full four-player match starts and finishes on LAN with the
internet unplugged, and the account layer may not break that. General Santos City is why.

⚠️ **LAST TSINELAS STANDING has rules, tests and a document and no match half**, and it is
deliberately absent from the lobby's RULES row until it has one. § 115.10, in the archive.

---

## How this file stays short

⚠️⚠️ **IT REACHED 22,930 LINES AND STOPPED DOING ITS JOB, WHICH IS TO BE READ.** 🧑, 2026-09-03:
*"todo md so long can u clean that shit up"*, *"its not supposed to be that long"*, and the
instruction that shaped the split: *"no ned to delete the batch reports ... js idk rename them or
smth"*, *"make the todo md that will be read very brief"*, *"future proof docs to not clutter like
taht again"*.

**So the rule, and it is mechanical on purpose:**

1. **A section lives here while its HEADING says `OPEN`, `IN PROGRESS` or `NOT DONE`.** Nothing
   else is consulted. Status in the heading, never buried in the prose, because prose is what made
   134 sections impossible to sort.
2. **When you finish something, change its heading and move the whole section to
   [`TODO_Archive.md`](TODO_Archive.md)**, keeping its number, and leave its row in the archive
   index at the bottom of this file. ⚠️ **Do not delete it and do not summarise it away.** The
   reasoning is the part that stays valuable, and every ⚠️ in this repository was written because
   something went wrong once.
3. ⚠️⚠️ **A SESSION REPORT IS NOT AN OPEN ITEM.** *"The 2026-08-29 evening batch"* was 525 lines,
   *"the 2026-08-29 balance-and-controls batch"* was 973, and neither was ever open work: they were
   records of a day. **Write the batch report, then archive it in the same commit.** Twelve of the
   twenty biggest sections in this file were dated batch reports.
4. ⚠️⚠️ **THE QUEUE AT THE TOP IS AN INDEX AND THE NUMBERED SECTIONS ARE THE EVIDENCE.** Start a
   session with `CURRENT IMPLEMENTATION QUEUE`; use the detailed section each row points at for the
   measurements and the acceptance criteria. **Do not resurrect archived work unless current code,
   a current test, or a row in the queue gives a concrete reason.** When something closes, take its
   row OUT of the queue in the same commit that archives its section, or the index starts lying in
   the one direction nobody checks.
5. **The numbers are not unique and that is not being fixed.** § 53, § 63, § 64 and § 65 each
   appear more than once. Renumbering would break every pointer in `CLAUDE.md`, `VISION.md`,
   `FUTURE.md` and the code comments, which is a worse trade than a duplicate heading. **Search by
   title as well as by number.**

---

## 142 · CONTROLLER SUPPORT: A PICTURE OF THE PAD, A PAD THAT CAN LEAVE A SCREEN, AND AN UNRECOGNISED PAD THAT WORKS ⚠️⚠️ OPEN, 2026-09-04, merged to `main`

🧑 2026-09-04, with a labelled line drawing of a DualShock attached: *"we will now be
implementing controller support. create a menu for controller mapping and ensure controller
support works in the game. use the picture as reference."*

⚠️⚠️ **HALF OF "CONTROLLER SUPPORT" WAS ALREADY BUILT AND SAYING SO MATTERS, BECAUSE THE OTHER
HALF WAS NOT WHERE ANYBODY WAS LOOKING.** § 139.2 makes the same point about per-device
rebinding: *"None of that needed building."* Before this pass the pad already had a binding for
every verb (`InputCatalogue`'s compile gate guarantees it), menu focus and thumb targets on every
screen (`ScreenFocus`), glyph-swapping prompts (`LastInputDevice`), per-device rebinding
(§ 125.13), rumble, and a warning for an unmatched device (§ 138.4 step 1).

**What it did not have was a way out of anything, a pause, or a pad that Unity does not know.**

---

### 142.1 ⚠️⚠️ EVERY BACK-OUT IN THE GAME WAS KEYBOARD-ONLY, SO A PAD COULD REACH EVERY SCREEN AND LEAVE NONE OF THEM

**Eleven call sites, all of them `Input.GetKeyDown(KeyCode.Escape)`:** `ConvertedScreen.Update`
(which is every converted screen in the game at once), `PlayerHub`, `SignInScreen`,
`CustomCharacterScreen`, `CustomGameScreen`, `WoodDropdown`, `LobbyChat` twice, `RoleSwapCard`,
`ConvertedSettingsPanel` twice, and `TouchLayoutScreen` through `Keyboard.current` instead.

⚠️⚠️ **`docs/TODO.md` § 138.2'S TABLE MISSED THIS AND THE REASON IS WORTH KEEPING.** That audit
walked every `<Gamepad>` binding path and every `Gamepad.current` read, which is the right sweep
for the question it was asking and **cannot see a literal keyboard read**, because a literal has
no binding to find. The same hole is § 35.3's (nine spectator keys outside the map) and
`Hud.EnsureSandboxToggle`'s F1 collision (three readers, none of them in the map). **A rule
asserted over the input asset cannot see a control that never reached the input asset.**

⚠️ **AND IT IS `CLAUDE.md` § 6.3 FAILING ON A WHOLE DEVICE AT ONCE:** *"Escape backs out on every
screen, always, innermost layer first... A player who learns Escape is reliable and then meets one
screen where it is not has learned that it is unreliable."* A pad player never got to learn it.
The same section calls a dead end a bug, and this was every screen in the front end.

✅ **`InputLayer.MenuNav` is the one place that answers it now**, and all eleven go through it.
It is Escape **or** the UI map's own Cancel, which is B on a pad.

- ⚠️⚠️ **IT IS NOT A NEW BINDING AND MUST NOT BECOME ONE.** B is `ReadyUp` in the PLAYER map.
  Backing out of a screen is the UI map's `Cancel`, which `UiInputModule` already keeps separate
  on purpose: *"Two maps, two contexts, exactly as `CLAUDE.md` § 4 describes for the spectator
  set."* Nothing was added to `Rebinding.RebindableActions` for this.
- ⚠️⚠️ **THE LEGACY `Input.GetKeyDown` SURVIVES INSIDE IT AND IS NOT A LEFTOVER.** Unity reports
  **Android's hardware BACK button** as `KeyCode.Escape` through the old manager and does not
  surface it as a `Keyboard` key at all. Swapping it for `Keyboard.current.escapeKey` would
  compile, read better, and silently take the back button away from every phone player.
  **`TouchLayoutScreen` had exactly that fault**: the one screen that exists because the player
  has no keyboard was the one screen a phone could not leave.
- ⚠️ **ONE CALL SITE IS DELIBERATELY LEFT ON ESCAPE**: `ConvertedSettingsPanel.Update`'s
  cancel-a-pad-rebind branch. The rebind operation already cancels through `<Gamepad>/buttonEast`
  itself, so routing that line through `MenuNav` would run `CancelRebind` twice on one press of B,
  with two `MenuSfx.Back()`. The comment there says so.
- ⚠️ **`RoleSwapCard` TAKES SUBMIT AS WELL.** The warmup buffer card taught
  `[SPACE] / [CLICK] TO DISMISS`, is shown DURING a match so it is not focusable and has no
  button to move to, and a pad player could only sit and wait it out.

### 142.2 ⚠️⚠️ THERE WAS NO PAUSE ON A PAD, AND `SpectatorPause` HAD TO MOVE FOR THERE TO BE ONE

`PauseWatcher` read `Input.GetKeyDown(KeyCode.Escape)` and nothing else, so **a controller player
could not leave a running match**: not resume, not settings, not quit to menu.

✅ **`Pause` is a real action now**, `<Keyboard>/escape` and `<Gamepad>/start`, in
`Rebinding.RebindableActions` under ROUND AND SCREEN, answered for in `ScreenInputCatalogue`, and
checked by `FindDuplicateBindings` like everything else. That is § 35.3's lesson applied a second
time: those nine spectator keys were *"not rebindable, not visible in the panel, and not checked by
anything"*, and this was the tenth.

⚠️⚠️ **`SpectatorPause` MOVED OFF START TO `<Gamepad>/buttonSouth`, AND THE TWO COULD NOT SHARE IT
EVEN THOUGH THE CONTEXT RULE LOOKS LIKE IT ALLOWS IT.** `PausePanel.OnOpened` renames its own card
to BROADCAST MENU when `GameLaunch.Spectator` is set, so **`PauseWatcher` serves a spectator too**
and both readers of Start would have been live on the same frame for the same person. That is
precisely the R collision `Settings.Rebinding`'s class note records (*"both sides of it are live in
the same context"*), not the legal kind its `SpectatorContext` set describes. Which one moves is
also written down there: *"when two must part, the one with fewer readers moves."* Every player
reaches the pause menu; the tactical pause is an operator key.

### 142.3 THE MENU HE ASKED FOR: `InputLayer.ControllerMapScreen`

⚠️⚠️ **A PICTURE, NOT A BETTER LIST, AND THE REASON IS THE QUESTION.** The settings GAMEPAD page is
a column of action names against a column of control names, and it answers *"what is LUNGE bound
to"*. **Nobody has that question.** The question a player holding a pad has is *"what does this
button under my thumb do"*, and a list can only be read that way round by somebody who already
knows the answer. The reference 🧑 attached is a labelled diagram for exactly that reason.

**What it is:** the controller in the middle, nine callouts down each side, a leader line from each
callout to the control it names, and **every callout is a rebind button**.

- **`tools/build_controller_diagram.py` draws it**, in `#55290F` ink on `UiTheme.Paper` with a
  `PaperSunk` touchpad. ⚠️ The reference is a photocopy whose face buttons are PlayStation cyan,
  pink and blue, which is three separate § 6.4 bans in one picture; **the four faces are told apart
  by SHAPE**, which is how the real pad does it and is `FUTURE.md` § 16.1's rule.
- ⚠️⚠️ **THE PICTURE AND THE ANCHOR TABLE COME OUT OF ONE PASS, AND THAT IS THE WHOLE REASON A
  LEADER LINE CAN BE TRUSTED.** If the drawing were generated and the arrow-heads typed into C#,
  moving the d-pad two hundred pixels left would be a change to one file that silently makes four
  lines in another point at bare plastic. `PadDiagram` reads the emitted manifest.
- ⚠️⚠️ **NOTHING ON IT IS A LITERAL.** Every label is resolved live, backwards, from the asset:
  the screen walks the actions and asks each for its **`effectivePath`** on the pad, so a rebind
  made on the settings page moves a label here and one made here shows there. A diagram with THROW
  painted beside the right trigger would be `VISION.md` § 3's *"screen that teaches the wrong key"*
  in the most convincing possible costume.
- ⚠️ **THE TWO STICKS ARE SHOWN AND NOT PRESSABLE.** `Move` and `Look` are not in
  `RebindableActions` and `ResolveBindingIndices` deliberately refuses to hand the stick to any
  direction, but a pad diagram with both sticks blank is a diagram of a pad nobody is holding.
  They read as jobs and refuse the press, which is § 6.3's *"a control that does nothing must not
  look pressable"*.
- ⚠️ **THE SPECTATOR SET IS DELIBERATELY ABSENT AND A FOOTNOTE SAYS SO.** Nine of these controls
  carry a second job while watching; drawing both would put two labels on most of the pad, which is
  § 6.2's third claim on the one screen that exists to be scanned.
- ⚠️ **ONE DOOR**, a row in the settings CONTROLS list beside the touch rows. § 6.3: *"NEVER ADD A
  SECOND DOOR TO FIX A FINDABILITY PROBLEM. That is exactly how § 92's six-button panel happened."*

⚠️⚠️ **AND THE RENDER FOUND FOUR FAULTS THE SOURCE COULD NOT, WHICH IS `CLAUDE.md` § 6.1 EARNING
ITS PLACE AGAIN.** Every one of them looked completely fine in the code:

| What the picture showed | The cause |
|---|---|
| **No leader lines at all**, on a screen whose whole point is leader lines | `Resources.Load<TextAsset>("UI/input/pad_diagram_v1")` resolved the **PNG** of the same basename and answered **null**. No error, no log. The manifest is `pad_diagram_v1_anchors.txt` now. **`Resources.Load` matches the path first and the type second** |
| **Still no leader lines** after that was fixed | `SetAsFirstSibling` was meant to put a line under the callout it starts from, and put all eighteen **under the opaque full-screen ground**. "First sibling" is not "under the callouts", it is under everything. They have a named layer between the drawing and the callouts now |
| **A 640-unit controller in a 980-unit hole** | The generated PNG had 35 per cent transparent margin, so `preserveAspect` fitted the CANVAS rather than the pad. § 6.2c's second question: *"is this image fitted to the region it is SEEN in?"* The generator crops to its own ink and remaps the anchors in the same pass |
| ⚠️⚠️ **Two lines running diagonally across the drawing and crossing four others** | The ring's order was typed in **under a comment claiming it was sorted by where each control sits on the pad**, and it was not: SELECT and START were at the bottom of their columns and their anchors are near the top. **A comment asserting a property the data does not have.** The order is now SORTED by the anchor's own Y, so the claim is arithmetic |

⚠️⚠️ **AND THE REBIND ITSELF IS `Settings.RebindSession` NOW, SHARED WITH THE SETTINGS PANEL.**
`ConvertedSettingsPanel.BeginRebind` was the only rebind in the game and carried **eight ⚠️ notes,
every one of them a fault somebody hit**: the target index must be the page's device or the
operation quietly edits the keyboard (§ 125.6), the candidates must be restricted or a keyboard
press on the pad page silently rewrites a key, the action must be disabled or the captured press
also fires the verb, the applied override must be REMOVED before the conflict check or a refusal
leaves two verbs sharing a control. **A copy would have had seven of them the next time somebody
found the ninth.** § 38.5's three dead protocols are what a second path costs here.

### 142.3b ⚠️⚠️ THE EMOTE WHEEL OPENED ON A PAD AND COULD NOT BE STEERED BY ONE

`EmoteWheel.Update` reads `Mouse.current.delta` and returns early on `Mouse.current == null`, so
on a controller the wheel **opened on the d-pad and then sat there**: no slice could be
highlighted, and releasing always played nothing. Its own class note is the sharpest part of it,
because it describes the fix while not having it: *"The wheel accumulates deltas into a stick-like
vector **exactly as a controller would drive it**."* The abstraction was right and the one device
it was shaped for was never wired in.

⚠️ **AND THE PAD IS NOT A DELTA.** A mouse reports motion and a stick reports a POSITION, which is
`InputAssetSync.LookAction`'s note one screen over: *"binding both to one action would make
`ReadValue<Vector2>` mean two different physical quantities."* So the stick SETS the vector and the
mouse ACCUMULATES into it, and a player who nudges the stick and then moves the mouse gets the
mouse's answer rather than a fight between the two.

### 142.3c ⚠️⚠️ THE PAD IS A REAL ILLUSTRATION NOW, AND IT IS CC0 RATHER THAN "OFF GOOGLE"

🧑 2026-09-04, of the first version: *"change the assets for controller map ... change also how
the controller looks like"*, and *"download it off of google"*.

**The first pad was drawn from primitives** — rounded rectangles, an ellipse per stick, a polygon
for the d-pad. It read as a controller and it read as programmer art, which is what it was. It is
now [Grumbel's PlayStation 3 gamepad](https://commons.wikimedia.org/wiki/File:PlayStation_3_gamepad.svg)
from the Open Clip Art Library via Wikimedia Commons.

⚠️⚠️ **CC0, AND THE SEARCH WAS RESTRICTED TO CC0 SOURCES ON PURPOSE.** The ask was to take art off
the web; **`docs/Asset_Sourcing.md` § 1 rule 1 is what decided WHICH art**, because an arbitrary
image result is almost always somebody's copyright and this repository is public. Rule 8 is what
permits it to be committed at all: *"CC0 and CC BY source may live in this public repository with
the proper licence."* The licence is `tools/assets/ps3_gamepad_cc0.LICENSE.txt`, the entry is
`Asset_Sourcing.md` § 8.1, and **no credits line is owed** because CC0 requires none.

⚠️⚠️ **IT IS RECOLOURED ON THE WAY IN AND THE RAW ART NEVER SHIPS.** The source is near-black with
**green, magenta, red and purple** face buttons: magenta is the exact fault § 139.4 records
shipping in the settings panel, and the other three each belong to another job in this palette.
`build_controller_diagram.py` maps every pixel onto the warm ramp **by luminance**, which keeps all
seven values of the illustration's modelling instead of flattening it, and forces the four face
glyphs to Honey Quartz so they are told apart by SHAPE. Same call, same reasoning, as
`tools/build_input_glyphs.py` made about the bought glyph sheets.

⚠️ **THE ANCHORS SURVIVED THE SWAP BECAUSE FOUR OF THEM ARE FOUND RATHER THAN TYPED.** The
generator locates the four face buttons **by their source hues, before the recolour destroys
them**. The other fourteen were measured by hand off a coordinate grid, which is why the tool
asserts the source raster's exact size: a re-render by a different SVG engine is a different set
of pixels and every leader line would move with nothing failing.

⚠️⚠️ **AND THE REAL ART BROKE THE ROW ORDER, WHICH IS THE INTERESTING PART.** § 142.3 had just
replaced a typed-in order with a SORT by each anchor's Y. That was right for the drawn pad,
whose controls sat in two tidy vertical bands, and **wrong the moment the photographed one
arrived**: SELECT and START sit near the CENTRE, much further from their columns than the d-pad
or the sticks, and a far target sandwiched between two near ones by height forces its line to cut
across both. Four crossings, all four those two labels. **The order is the target's ANGLE seen
from its own column now**, because a fan sorted by angle from one origin cannot cross.

⚠️⚠️ **AND THE ANGLE ALONE WAS STILL NOT ENOUGH, WHICH IS WORTH THE EXTRA PARAGRAPH.** That
theorem holds for lines leaving ONE point; these leave a column spread over the whole 750-unit
gutter, so for two targets at nearly the same angle the order can still come out inverted. It
did, on the very next render: HIDE HUD's leader ended exactly on the point ABILITY INFO's leader
passed through. `ControllerMapScreen.Uncross` now bubbles adjacent rows while an actual
**segment-intersection test** says their two lines cross, so *"no reader has to trace a line with
a finger"* is a property of the output rather than a promise in a comment. The angular sort stays
in front of it, because it leaves the pass almost nothing to do.

### 142.3d ⚠️⚠️ THE PAD IS THE DUALSHOCK 4 LAYOUT NOW, AND THE LEADERS BEND

🧑 2026-09-04, with two reference pictures: *"use the first pic instead of creating your own
controller model for the mapping"*, and *"fix the lines pointed to the buttons so it's not
straight."*

**The art.** The reference is a modern pad with a touchpad and a slab body, and the CC0 PlayStation
3 drawing § 142.3c had just landed was neither. It is
[Tokyoship's Dualshock 4 Layout](https://commons.wikimedia.org/wiki/File:Dualshock_4_Layout.svg)
now: same generation as the reference, same touchpad, same silhouette, and a LINE drawing rather
than a solid, which is what the reference is.

- ⚠️⚠️ **IT IS CC BY 3.0, NOT CC0, AND THAT IS A BILL RATHER THAN A DETAIL.** Rule 1 excludes
  paid, non-commercial and **CC BY-SA**; it does not exclude CC BY. What CC BY costs is a CREDIT,
  and `Asset_Sourcing.md` § 9 is explicit that a line goes in only when the asset actually ships.
  This one ships. **Removing the credit and keeping the picture is a licence breach, not a
  tidy-up.**
- ⚠️ **A CC0 DUALSENSE DOES NOT EXIST TO FIND.** Commons has no DualSense SVG at all and its one
  PS5 render is CC BY-SA, which rule 1 excludes outright. This is the closest freely licensed
  drawing to the picture handed over.
- ⚠️⚠️ **THE PLAYSTATION ROUNDEL IS ERASED ON THE WAY IN.** A licence to reuse somebody's DRAWING
  is not a licence to the trademark inside it, and this repository already carries one open item
  of that exact shape: `docs/Port_Plan.md` § 8 lists the IKE slipper first in the replacement queue
  because it *"carries the real Nike wordmark as geometry"*. SHARE and OPTIONS stay; they are
  ordinary words naming a button.
- ✅ **THE CREDIT IS IN THE GAME**, in `CreditsContent.CcByCredits` beside the three slippers,
  which are the same deal. `Asset_Sourcing.md` § 8.1 and § 9 carry it too.
- ⚠️ **THE RAMP INVERTED WITH THE ART.** The PS3 pad was a dark solid and its ramp bottomed out
  near black so it would read on honey paper. This one is linework on a light body, so the top of
  the ramp is pushed to `UiTheme.Paper`, **lighter than the page**, and the pad reads as an object
  lying on the paper rather than a hole cut in it.

⚠️⚠️ **AND THE AUTOMATIC FACE-BUTTON SEARCH DIED WITH THE OLD ART, WHICH IS A REAL LOSS AND IS
WRITTEN DOWN RATHER THAN GLOSSED.** The PS3 drawing coloured its four face buttons, so the
generator FOUND them and could not mistype them. This one draws them as outlines like everything
else, so all eighteen anchors are typed. Two things replace it, and neither is as good:
`check_anchors_land_on_the_pad` fails the build when an anchor is off the drawing, and `--preview`
writes an **overlay with a ring at every anchor**. ⚠️ **That overlay is not a nicety, it is the
measurement**: the first eighteen numbers were read off a coordinate grid by eye and every one was
forty to a hundred pixels out, which the drawing hid completely and the overlay showed at a glance.

**The lines.** They were single diagonals; they are **elbows** now, a long run level with the
label and a 45-degree tip into the control, which is what the Xbox diagram he attached does and
what every technical drawing does. The reason is not taste: a straight diagonal arrives at
whatever angle the two points happen to make, so eighteen of them arrive at eighteen angles and
the eye reads a starburst. With the long part of every line parallel, each label owns one line.

- ⚠️ **`PathsCross` TESTS THE ELBOWED PATH, NOT THE CHORD.** The uncrossing pass added in § 142.3
  compared straight lines, which WERE the leaders then. A bend moves a line by up to its own drop,
  so two leaders whose chords miss can have elbows that meet, and a pass still testing chords
  would call that clean.
- ⚠️⚠️ **AND THE ROW HEIGHT AND THE DIAGRAM WIDTH ARE ONE DECISION, WHICH THE FIRST RENDER OF THIS
  ART PROVED.** `DiagramSize` fits the pad inside the callout band, so a band much taller than the
  pad leaves the top and bottom rows level with bare paper and their leaders travelling the whole
  height of the picture to reach anything. The DualShock's 1.6 aspect gave a 610-unit pad in a
  748-unit band and **four long diagonals straight across the drawing**. Matched at 685 against
  676, every tip is short.

### 142.3e ⚠️ A TILTED PAD WAS BUILT AND REVERTED, AND THIS IS THE RECORD SO NOBODY REDOES THE HUNT

🧑 2026-09-04 asked for a pad *"tilted like this ps5 controller"*, then for it to be white, then:
*"just revert changes to this"*, pointing at the flat drawing above. **It is his call and the
flat one is what ships.** The two commits are reverted rather than deleted, so everything below is
recoverable with `git show`.

**What was built** (`ba1934b`, then `11ed878`, both reverted by this entry's commit):

- The art was TheHoodieGuy02's Dualshock 4 sheet from the
  [8th Gen Console Vector Gamepad Collection](https://opengameart.org/content/8th-gen-console-vector-gamepad-collection),
  **CC0**, drawn with the shoulder buttons' top faces visible.
- ⚠️⚠️ **THE ONE THING THE TILT BOUGHT THAT THE FLAT DRAWING CANNOT**: on a front view the
  shoulders are edge-on, so **L1 and L2 are one visible bar** and § 142.3d's anchor table has to
  spread two labels along it and say so. The tilted art gives each its own point. If that
  compromise ever reads wrong on screen, this is the fix and it is one commit away.
- It was CC0, so it **removed** the CC BY credit obligation the current art carries. Reverting
  brings that obligation back, and the line is back in `CreditsContent.CcByCredits` where it
  belongs. ⚠️ **The current pad is CC BY 3.0 and owes that credit for as long as it ships.**

⚠️⚠️ **AND ONE FINDING FROM IT IS WORTH KEEPING EVEN THOUGH THE CODE IS GONE, BECAUSE THE NEXT
SOURCE SWAP WILL MEET IT.** Making that art light by **inverting the luminance destroyed the
silhouette**: it was a filled drawing whose outlines and whose button recesses were both pure
black, and inversion cannot tell those apart, so the pad's outer edge went to near-white and the
shape dissolved into the page. **A filled source has to be mapped by ROLE, not by an inverted
ramp**, and only a render says so.

### 142.3f ⚠️⚠️ THE PAD GLYPHS ARE PS4 PROMPTS NOW, AND THE OLD ONES WERE XBOX ON A DUALSHOCK

🧑 2026-09-04, with a sheet of PlayStation prompts: *"change the control icons to these. look in
the internet for the icons. do not replicate it yourself"*, then *"it should be the ps4 icons"*.

**The map drew a DualShock and labelled it `Y`, `B`, `A`, `X`.** The pad half of `UI.InputGlyphs`
was the Xbox side of vryell's pack: two vocabularies for one device, on one screen, which is
`docs/VISION.md` § 3's rule about teaching the wrong control in a costume nobody would grep for.

**The source is [Kenney's Input Prompts 1.5](https://kenney.nl/assets/input-prompts), PlayStation
Series, CC0**, nineteen files committed under `tools/assets/kenney_ps4/` with the pack's own
licence text. ⚠️ **Only TWO of the nineteen are PS4-specific**: Kenney draws the shapes, triggers,
sticks and d-pad once for every PlayStation generation, and SHARE and OPTIONS are the pair that
changes. Moving the diagram to a DualSense is a two-file swap.

⚠️ **THE ART IS TINTED, NOT USED AS SHIPPED.** The prompts are pure white on transparent, so
`tools/build_pad_prompt_icons.py` bakes two rows — ink for paper, cream for the in-match HUD —
because `InputGlyphs.For` promises its callers a sprite that is already the right colour and they
set `Image.sprite` without ever touching `Image.color`. ⚠️⚠️ **The d-pad's highlighted arm keeps a
colour of its own** (Persimmon, § 6.4's "marker"): flatten it and all four directions become the
same picture, which is the fault `InputGlyphs` already had a warning about for the old sheet.

⚠️⚠️ **AND THE SWAP DELETED MORE CODE THAN IT ADDED, WHICH IS THE PART WORTH KEEPING.** The bought
sheet's layout forced a dark/light row pairing, two column-offset constants, and a `DPadColumn` /
`DPadRow` pair whose own note had to explain that *"the same direction is at column 2 on one and
column 1 on the other"*, because the bare row carried an extra all-lit cell the outlined row did
not. **This sheet is generated by this repository**, so its grid is one column per control and one
row per ground, and none of that arithmetic exists. `Sheet.Pad` and `Sheet.Stick` are gone;
`Table` is the keyboard and mouse now.

⚠️⚠️ **TWO IMPORTER TRAPS, AND ONE WOULD HAVE BEEN SILENT.** `EditorTools.InputGlyphImport` caps
everything in that folder at **512 px** and forces **Point** filtering, both correct for 16 px
cells sliced out of a small sheet:

- **The sheet is 1216 wide.** Unity would have halved it twice with no error, and `InputGlyphs`
  slices by `column * 64`, so every glyph past the second would have come from the wrong cell or
  off the end. That is the `npotScale` fault the same file already records, one setting over.
  `InputGlyphTests` asserts the width for exactly this reason.
- **Point is right for an upscale and wrong for a downscale.** The old cells were 16 px drawn at
  34 units; these are 64 px drawn at about 46. Same folder, opposite answer, because the direction
  of the scale is what decides it.

⚠️ **AND THE TWO SHEETS IT ORPHANED ARE DELETED RATHER THAN LEFT IN `Resources`.**
`glyphs_pad_v1` and `glyphs_stick_v1` had no reader once `Sheet.Pad` and `Sheet.Stick` went, and
anything under `Resources` ships whether it is loaded or not. `tools/build_input_glyphs.py` no
longer lists them either, with a note saying why it still exists: it is the only thing that makes
the KEYBOARD and MOUSE sheets, which are unchanged.

⚠️ **WHAT IS STILL OPEN HERE**: a player on an **Xbox** pad now sees PlayStation glyphs. Kenney's
pack ships the Xbox series in the same grid, and the Input System already knows which pad is
attached (`Gamepad.current` is an `XInputController` or a `DualShockGamepad`), so picking the
sheet by device is a small, well-defined follow-up rather than a redesign. It was not done because
it was not asked for.

### 142.3g ⚠️⚠️ THE DEVICE SWAP WAS ONLY HALF WIRED, AND ANSWERING "DOES IT WORK" FOUND THREE HOLES

🧑 2026-09-04 asked whether the game switches icons automatically between keyboard and pad.
**Tracing it rather than assuming found three gaps, and all three are fixed.**

**What already worked:** `LastInputDevice.Sample` polls touch, then pad with an actuation check,
then keyboard; `Hud.KeyLabel` branches on it and returns the GAMEPAD binding's name, cached and
invalidated on `LastInputDevice.Revision`. So the text prompts did swap.

#### 142.3g.1 ⚠️⚠️ THE DETECTION NEVER RAN IN THE MENUS

`LastInputDevice.Sample()` had **exactly one caller**: `PlayerInputReader.Update`, a component that
only exists on a locally-driven seat **inside a match**. In the entire front end nothing moved
`Current`: it sat on whatever `Seed()` chose at boot or whatever the last match left behind.

⚠️ **THAT IS THE FEATURE'S OWN HEADLINE CLAIM FAILING.** `docs/FUTURE.md` § 14 asks for glyphs
*"driven by the last device used, not by a setting"*; before a player's first match it was driven
by neither. A pad player opening SETTINGS got the KEYBOARD page, because `ConvertedSettingsPanel`
asks this exact question to choose one.

✅ **`LastInputDevice` installs itself on `InputSystem.onAfterUpdate` now**, which is the hook
`GenericPadBridge` reached for two entries ago **for the identical reason**, stated in its own
note: the only component that ticks input every frame lives in a match, and a pad has to work in
the menus. The `PlayerInputReader` call is deleted rather than left beside it.

#### 142.3g.2 ⚠️ THE ICONS ONLY SWAPPED IN THE TUTORIAL

`InputGlyphs.For` had two callers: `GuidedTraining.KeyCap` and the controller map. Everywhere else
the HUD draws the label as **text in brackets**, so `[SPACE]` became `[BUTTON SOUTH]` and no
picture switched.

✅ **The ability deck's key chip draws a glyph now**, which closes a complaint § 126.9 filed before
there was any art to fix it: the chip is *"sized for `Q`"* and `BUTTON WEST` in it is *"trading one
overflow for a worse one"*. ⚠️⚠️ **And it turned out not to refresh at all**: the text was written
ONCE at build and only its colour was touched per frame, so a player who picked up a pad mid-match
read `Q` under the tile for the rest of the round. That is the same fault `UpdateHeroDeck` records
fixing for `_inspectHint`, surviving one line away from its own fix. `RefreshKeyCaps` keys on
`Rebinding.Revision` and `LastInputDevice.Revision`, like everything else that caches a label.

⚠️ **THE MID-SENTENCE PROMPTS ARE STILL TEXT AND THAT IS A CONSTRAINT, NOT A CHOICE.**
`"MASH [" + KeyLabel(action) + "]"` builds a STRING, and this front end draws legacy
`UnityEngine.UI.Text`, which cannot inline a sprite. Giving those a picture means rebuilding each
one as a layout of image-plus-label, screen by screen. **The discrete key caps are done; the
sentences are not, and they are the ones a player reads once.**

#### 142.3g.3 ⚠️ AN XBOX PAD WAS SHOWN PLAYSTATION GLYPHS

Fixed by shipping both families in one sheet and picking at runtime:
`InputGlyphs.FamilyOf` asks whether the attached pad is a `DualShockGamepad`.

- ⚠️ **XBOX IS THE DEFAULT AND PLAYSTATION IS THE SPECIAL CASE**, which is the right way round
  here: everything Unity matches that is not a DualShock, **plus every pad `GenericPadBridge`
  stands in for**, presents as XInput-shaped. Guessing PlayStation would put a cross on exactly
  the no-name pads least likely to be one.
- ⚠️ **THE CONTROLLER MAP PINS ITSELF TO PLAYSTATION**, and it is the only caller that overrides
  the hardware. That screen labels its own picture, and the picture is a DualShock 4: a triangle
  beside a drawn triangle is coherent, an Xbox `Y` beside it is not.
- ⚠️ **BOTH FAMILIES COME OUT OF ONE GENERATOR PASS AND ONE COLUMN TABLE**, so the two orders are
  physically the same list. Two sheets from two passes are two orders that can drift.

### 142.4 ⚠️ AN UNRECOGNISED PAD IS DRIVEN NOW: § 138.4 STEPS 2 AND 3, BY A ROUTE § 138 DID NOT EXPECT

§ 138.4 step 2 asks for a registered fallback LAYOUT. ⚠️⚠️ **THAT ROUTE IS SHUT AND IT IS WORTH
WRITING DOWN SO NOBODY SPENDS A DAY REDISCOVERING IT.** Unity's HID support hangs off
`InputSystem.onFindLayoutForDevice`, and `InputManager` takes the **FIRST** callback that answers
(`if (!string.IsNullOrEmpty(newLayout) && !haveOverriddenLayoutName)`). HID's callback is registered
during the Input System's own static initialisation, which is triggered by the first touch of
`InputSystem` — **including the touch that would register ours** — so there is no order in which
this game's callback runs first. Out-scoring it with `RegisterLayoutMatcher` instead means beating a
matcher HID builds per device from the vendor id, the product id and the usage, at runtime.

✅ **So `InputLayer.GenericPadBridge` leaves the joystick where it is and creates a `Gamepad`
beside it**, copying one into the other from `InputSystem.onAfterUpdate`. That buys step 3 for
nothing, which is § 138.4's own argument: *"once a pad is a `Gamepad`-derived device the existing
GAMEPAD page already works."* **Nothing else in the repository learns a new concept**:
`LastInputDevice`, `ScreenFocus`, `Rumble`, every `<Gamepad>/` binding, both settings pages and the
new map all just work.

- ⚠️ **THE COST IS ONE FRAME.** A state event queued from `onAfterUpdate` is processed by the next
  update. A pad that is one frame late is a pad; a pad that is dead is a broken game.
- ⚠️⚠️ **AN EVENT IS QUEUED ONLY WHEN SOMETHING CHANGED**, and that is what keeps `Gamepad.current`
  honest. `current` is whichever pad last received an event, so a bridge pumping sixty identical
  events a second would steal it from a real controller plugged in beside it and hold it for ever,
  and would flip `LastInputDevice` to pad glyphs with nobody touching the thing.
- ⚠️ **`onAfterUpdate` RATHER THAN A `MonoBehaviour`.** The one component that already ticks input
  every frame is `PlayerInputReader`, which only exists on a seat inside a match. A pad has to work
  in the MENUS, which is where the player plugs it in and where they go looking when it does
  nothing.
- ⚠️⚠️ **THE MAPPING IS A GUESS AND THE MAP SCREEN IS ITS CURE**, which is the deal § 138.4 already
  struck: *"it will be wrong for some pads and right for many, and a wrong mapping the player can
  SEE beats a dead pad they cannot."* The order is the XInput-style DirectInput one (A, B, X, Y,
  two bumpers, two triggers, SELECT, START, two stick clicks); **the PlayStation-style families
  disagree and come out rotated**, and the fix is two presses on the map.
- ⚠️⚠️ **IT CAN BE SWITCHED OFF, AND THAT IS NOT HEDGING.** A flight stick or a racing wheel is
  also an unmatched `Joystick`, and bridging one puts a throttle axis on the movement stick and
  holds a verb down for the whole match. Nothing in the descriptor tells the two apart, so the
  answer is a switch the player can find: a row in the settings CONTROLS list, drawn **only** when
  an unrecognised device is actually attached.
- ⚠️ **`ControllerWatch.StatusLine` CHANGED WITH IT.** It read *"so it will not work"*, which was
  true in the morning and false by the afternoon. **A screen that tells a player their working
  controller is broken sends them to unplug it.**

### 142.4b ⚠️ HOW THIS PASS WAS VERIFIED, AND THE ONE SUITE THAT COULD NOT BE

- **EditMode: 355 tests, 1 failure**, and that one is the Mac's own `QualitySettings` churn (see
  § 142.5). The seven new assertions live in `ControllerSupportTests`.
- ⚠️⚠️ **THE INTERESTING ONE IS `EveryScreenBacksOutThroughTheOneReaderRatherThanAKeyboardLiteral`,
  WHICH READS THE RUNTIME SOURCES AS TEXT.** `InputSurfaceCheck`'s reason applies exactly: no
  running test can see a screen nobody opened, so a twelfth `GetKeyDown(KeyCode.Escape)` added next
  month would be as silent as the eleven were. ⚠️ **Its first version failed on two COMMENTS** that
  name the literal in order to explain why it is gone — which is `tools/audit_audio_reach.py`'s
  lifelong bug (*"the only audit that did not strip comments before looking for a gate"*) reproduced
  within an hour of reading about it. It strips line comments now.
- **`Checks.RunAll`: all 7 green in one launch**, including `InputSurfaceCheck`, which is what
  proves the new screen goes through `MenuKit` rather than building a bare canvas.
- **Renders: `Logs/shots-runtime/ControllerMap-v95.png` and `-ShortWide.png`**, the second at
  1920x820 because § 6.2b's third row is the one this repository gets wrong most often.
  `UiRuntimeShots.Capture` takes a size now; every existing call keeps 1920x1080.
- ⚠️⚠️ **AND ONE LAUNCH ON THIS MAC SIMPLY HUNG, WHICH IS WORTH THE THREE LINES IT TAKES TO
  RECOGNISE.** It logged `[UnityConnect] An error occurred` after a `cdn.cloud.unity3d.com` HTTP
  299, wrote **956 log lines and then nothing for fifteen minutes**, while `ps` showed the process
  at 160 per cent CPU. That reads exactly like a long import and is not one: `sample` on the pid
  put the **main thread parked in `ReceiveNextEventCommon`**, the Cocoa event loop, with the CPU
  all on worker threads. **A batchmode Unity whose main thread is idle is finished or stuck, never
  busy.** Killing it, clearing `Temp/UnityLockfile` (`CLAUDE.md` § 7) and relaunching was enough.

- ⚠️⚠️ **THE FULL PLAYMODE SUITE COULD NOT BE RUN AND THAT IS § 126.8 RATHER THAN THIS PASS.** It
  came back `total="0"` with a `MissingReferenceException` inside `QueueCardLayoutProbe.Measure` on
  a destroyed `RectTransform`: **the whole run died on one fixture's polluted world**, which is the
  third state `CLAUDE.md` § 7 warns about and § 126.8's exact signature. This file's own header
  already prescribes the way round it — *"Verify with `-testFilter` over the suites you touched"* —
  and that is what was done.

### 142.5 ⚠️ WHAT IS STILL OPEN

- ⚠️⚠️ **🧑 HAS NOT LOOKED AT IT, AND THAT IS THE ACCEPTANCE TEST.** `CLAUDE.md` § 6.2 is his three
  claims and none of the three is visible to any probe in this repository. **This entry stays OPEN
  until he has opened a build, held a pad, and said the map is right.**
- ⚠️⚠️ **AND NOBODY HAS HELD AN UNRECOGNISED PAD AGAINST `GenericPadBridge`.** § 138.3 was already
  blunt about this (*"NOBODY HAS TESTED ANY PAD ON THIS PROJECT EXCEPT THE ONE ON THIS DESK"*) and
  it is still true. The bridge is asserted in EditMode against a synthetic joystick, which proves
  the wiring and not the guess. **§ 138.4 step 4, a written list of real pads with vendor and
  product ids in [`../Attention.md`](../Attention.md), is the open half and it needs hardware.**
- **The four face buttons are drawn as PlayStation shapes and named by compass position.** That is
  honest on both families and slightly foreign to each. If it reads wrong to him, the drawing is
  one Python function.
- ⚠️⚠️ **AND ONE TRAP THAT IS THE MACHINE RATHER THAN THE CODE, WRITTEN DOWN BECAUSE IT WILL BITE
  THE NEXT SESSION ON THIS LAPTOP.** Every `-batchmode -runTests -nographics` EditMode run on the
  **Mac** editor rewrites `ProjectSettings/QualitySettings.asset`, dropping quality level 5
  (Ultra, the Standalone default) from `antiAliasing: 4` to `0`, and
  `QualitySettingsAssetTests.EveryStoredAntiAliasLevelMatchesTheDocumentedTable` then fails **on
  the run that caused it**. It is not a code defect: `git show HEAD:` has 4, the committed value
  is right, and that test's own message describes the mechanism (*"writing
  `QualitySettings.antiAliasing` during PLAY writes through to this asset"*). **`git checkout --
  ProjectSettings/QualitySettings.asset` after a run**, and do not commit the change or chase the
  red.

- ⚠️⚠️ **AND THE WINDOWS PLAYER WAS NOT BUILT FOR THIS PASS, BECAUSE THIS MACHINE CANNOT BUILD
  ONE.** The work was done on the **Mac** (`/Applications/Unity/Hub/Editor/6000.5.8f1`), whose
  installed playback engines are **MacStandaloneSupport and WebGLSupport only**: there is no
  Windows Standalone module, so `GameBuilder.BuildWindows` has no target to write. `CLAUDE.md`
  § 7's table describes the Windows laptop and is still right about it. **A Mac player was built
  and verified instead**; the Windows one has to come off the other machine, from this commit.

- ⚠️ **THE CALLOUTS ARE 76 UNITS TALL, UNDER THE 144-UNIT THUMB FLOOR**, and are padded out by
  `ScreenFocus.MakeRoomForThumbs` like the touch customiser's bar. Eighteen rows at 144 do not fit
  a 1080-unit canvas, and this screen exists for a device that has no thumbs on the glass. It will
  show up in the `ThumbFloor` sweep; see § 126.2 for why that number is a worklist and not a gate.

---

## 150 · THE CAMERA/FEEL/LIFECYCLE PASS: A HITSTOP THAT DRIFTED 11.9 m, A BEARING NOBODY PASSED, AND AN AUDIT THAT COULD NOT SEE ITS OWN WORST CASE ⚠️ IN PROGRESS, 2026-09-05, branch `main`

⚠️⚠️ **THE ANIMATION HALF OF THIS BRIEF IS NOT IN THIS FILE AND MUST NOT BE PICKED UP FROM IT.**
Hero cast animations, the ultimate comic-book cinematics and the retrieval slide's own clip are
owned by **Astra** and queued in [`../ASTRA.md`](../ASTRA.md), which carries the one-task-per-session
rule that queue runs under. This is the same split this file's own queue already applies to
controller support (§ 142, OWNED): **two people writing the same layer is worse than nobody
writing it.**

### 150.1 ⚠️⚠️ P1 CONFIRMED AND FIXED: THE HIT FREEZE WAS A RANDOM WALK, AND IT GOT WORSE ON A BETTER MACHINE

**The lead**, from the brief: normal follow resets the camera pose every frame, `StepShake()` adds
its punch with `transform.position += ...`, and `HoldFrame()` skips the follow while the shake
keeps running, so the offsets may accumulate instead of staying relative to a stable baseline.

**Confirmed exactly as stated.** `CameraRig.LateUpdate` calls `StepHold()` above everything that
writes the transform; a held frame returns before `ApplyFpp`/`ApplyTpp`, which are the absolute
writes the `+=` depends on. So on a held frame the punch had nothing to be relative to.

**The arithmetic, on the shipped weights** (`HitFeel.Weight.Ultimate`: a 0.11 s hold, and
`ImpactPunch` at `0.20 * 1.40 = 0.28 m` decaying over 0.16 s):

| | Frames inside the hold | Sum of `punchRatio` | Accumulated drift |
|---|---|---|---|
| 60 Hz | 7 | 4.08 | **1.14 m** |
| 144 Hz | 16 | 10.1 | **2.83 m** |

⚠️⚠️ **AND THE MEASURED NUMBER IS WORSE THAN THE ARITHMETIC, WHICH IS THE PROOF THAT THE CAUSE IS
THE FRAME RATE AND NOT THE WEIGHTS.** `HitFreezeProbe`, run in batch mode where the frame rate is
uncapped, measured **11.890 m against a 0.45 m ceiling**. A hold is a DURATION, so a shorter frame
buys more frames and every frame is another addend: **the better the machine, the further the
camera walked**, during the one beat whose entire job is to read as a dead stop, in a 14 m box
(`docs/VISION.md` § 2).

**The fix is the smallest one that restores the documented intent.** `HoldFrame`'s own header
already claims the view *"sticks where it was while the world carries on simulating underneath
it"*, and that was true of everything except the shake. `StepHold` now captures the pose on the
first held frame and restores it **before** `StepShake` each frame after. The punch and the shake
then behave exactly as they do on a normal frame: an offset from a stable baseline, bounded by
their own amplitude, independent of the frame rate.

⚠️ **RESTORED BEFORE THE SHAKE, NEVER AFTER IT.** Assigning the anchor afterwards would erase the
punch and produce the opposite bug, a hitstop with no impact in it. `HitFreezeProbe` asserts a
**band** rather than a ceiling for exactly that reason, so the wrong fix fails too.

⚠️ **AND `Follow()` NOW CLEARS THE HOLD, WHICH THE ANCHOR MADE MANDATORY RATHER THAN TIDY.** A
freeze still running across a seat change would pin the new seat's view to the OLD body's frozen
pose for the rest of it. Same argument as the `_fallView` line beside it and as § 149.8: state
whose meaning came from a body it no longer follows. `HitFreezeProbe
.AFreezeDoesNotSurviveOntoTheNextSeat` is the second case.

**Proved able to fail:** the probe was run against the fix commented out and reported **11.890 m**
before it was run against the fix and passed. A regression that has not been seen red is not a
regression.

### 150.2 ⚠️⚠️ P1 CONFIRMED AND FIXED: EVERY HERO SKILL HIT PUNCHED THE CAMERA THE WRONG WAY

`HitFeel.Land(victim, weight, accent, from)` takes the attack origin as its **fourth and optional**
argument, and its header states the job: *"so the hit has a bearing and not just a magnitude. A
player who knows WHERE it came from can turn."*

⚠️⚠️ **OMITTING IT IS NOT A MISSING EFFECT, IT IS A WRONG ONE.** `from` defaults to
`Vector3.zero`, `Land` reads that as "no direction" and punches along `-victim.transform.forward`
instead. **So the camera was shoved straight backwards relative to the victim's own facing on
every hit that did not pass an origin, and a player turning toward the hit turned away from it.**

**What the audit found, and the split is the interesting part:**

| Call site | Verdict |
|---|---|
| `CheskaHeroKit` (Glacial Nova), `DanteHeroKit` (Titan Fissure) | ✅ **Both passed `ctx.Position` and were correct the whole time.** |
| `MatchFlair.PlayHeroHit` | ⚠️⚠️ **Passed nothing, and it is the tail of FIVE call sites**, which between them are every skill-weight hero hit in the game. The caster was already in hand on the line above: `AccentOf(caster)` looks up the same body so the victim can tell WHO hit them. |
| `MatchFlair.PlayZap` | ⚠️ Passed nothing, and already had `at`, the hazard's own origin, as a parameter. |

**Fixed by passing what each site already had**: the caster's position for the direct hits, `at`
for the zone tick. ⚠️ **A null caster is a legal answer and stays `default`**, which `Land` already
documents as "there is nothing to turn toward"; `Seat(actor)` returns null for a caster this peer
has no body for, exactly as `AccentOf` handles.

⚠️ **IT LEAKS NOTHING, AND THAT WAS CHECKED RATHER THAN ASSUMED.** `Land` returns immediately
unless the rig is following the VICTIM, so this runs on the victim's machine about a hit they have
just taken. The attacker's screen is not touched, which is the rule `HitFeel`'s own header states
and the reason it exists.

**The regression is a CALL-SITE claim**, `NationalsHardeningTests
.EveryHitFeelCallSitePassesTheBearingItWasGiven`, which walks every `HitFeel.Land(` in `Runtime/`
and counts **top-level** commas (a nested `UiTheme.BrightForHero(id)` is the normal way the accent
is written at these sites, so counting every comma would have passed the exact bug). ⚠️ § 149.7's
rule says to KEEP this category: *"The compiler cannot see that something is NOT called"*, and an
optional argument nobody passes compiles perfectly and ships the wrong feedback.

### 150.3 ✅ § 149.8's OPEN HALF IS BUILT: THE SECOND MATCH IN ONE PROCESS

§ 149.8 fixed the defect (the single exit called `GameLaunch.Reset()` from nowhere) and closed
saying *"what is still open is the two-match integration run itself"*. `SecondMatchLifecycleProbe`
is it, four cases, and it drives **`SceneFlow.LeaveMatchToMainMenu`** rather than `GameLaunch.Reset`
directly. ⚠️ **That is the whole point**: the defect was never that `Reset` did the wrong thing, it
was that the exit never called it, so a test calling `Reset` itself passes against the bug. Same
lesson as `SessionRestartTests`' *"it asserts the second start, not the first"*.

| Case | What it holds |
|---|---|
| `LeavingAMatchClearsTheLaunchBlockAndKeepsTheProcessSwitch` | `Spectator`, `GuidedTutorial`, `PendingAction`, `PendingJoinAddress`, `SeatTokens` and `PracticeSandbox.Wanted` are all clear after the real exit, **and `GameLaunch.AllBots` is still set**, so somebody "tidying" it into `Reset()` fails here rather than silently making every multi-match harness measure three parked bodies |
| `AfterSpectatingOneMatchTheNextOneHasSomebodyInTheSeat` | The flag's EFFECT, not the flag: a fresh `MatchInstaller.HumanSeat` answers a real seat. ⚠️ `AllBots` is deliberately turned OFF for this one, because that switch legitimately answers -1 too and leaving it set would pass for the wrong reason |
| `TheAudioDirectorIsBuiltOnceForTheProcessSoItsSceneHandlerCannotAccumulate` | § 150.4 |
| `BotDifficultyIsReDerivedForEachMatchRatherThanInheritedFromTheLastOne` | § 150.5 |

⚠️ **THE FIRST RUN FAILED ON THE HARNESS AND NOT THE GAME, AND IT IS WRITTEN DOWN BECAUSE
`MatchSoakProbe` ALREADY LEARNED IT.** The seat probe built its `GameObject` **before** calling the
exit, and the exit LOADS A SCENE, so the object was destroyed and `AddComponent` threw
`MissingReferenceException` from the test rather than failing the assertion. *"A check whose first
run accuses the game of the harness's mistake teaches everybody to distrust the harness."*

### 150.4 ✅ MEASURED, NOT A BUG: THE `AudioDirector` ANONYMOUS `sceneLoaded` SUBSCRIPTION

The brief asked whether the owning object truly persists for the process or whether stale
subscriptions can accumulate. **It persists, and the claim is now asserted rather than argued.**

`AudioDirector.Awake` does `SceneManager.sceneLoaded += (_, __) => KeepOneListener();`, an
anonymous delegate that **can never be unsubscribed**. That is safe if and only if exactly one
`AudioDirector` is ever constructed, and it is: `GameServices.Ensure()` opens
`if (_root != null) return;` over a `DontDestroyOnLoad` + `HideAndDontSave` root.

⚠️ **THE TEST ASSERTS IDENTITY, NOT A COUNT, AND THAT IS FORCED.** The root is `HideAndDontSave`,
so `FindObjectsByType` **cannot see it**: `AudioDirector.Awake`'s own header records two enabled
listeners coexisting unseen for a whole session for exactly that reason, so a count would measure
zero and pass no matter what was true.

### 150.5 ✅ MEASURED, NOT A BUG: BOT DIFFICULTY IS RE-DERIVED RATHER THAN INHERITED

`AIController.ActiveDifficulty` and `AIController.BotsEnabled` are process statics and are **not**
in `GameLaunch.Reset()`. They are safe because **`MatchInstaller.Start` calls
`ApplyDifficultyFromSettings()` before it builds a single seat**, and every screen that chooses a
difficulty writes the saved setting first (`CustomGameScreen.SetBots` saves and then applies). So
the second match reads the setting rather than the leftover, and the lobby's choice is not
overwritten either.

⚠️ **IT IS WORTH A TEST ANYWAY**, per § 149's standing rule that a false positive closes with the
proof that it is one. If somebody removes that call from the installer, a NONE-bots practice match
would leak an empty arena into the next real one.

### 150.6 ⚠️⚠️ P1 CONFIRMED AND FIXED: THE SUBSCRIPTION AUDIT WAS BLIND TO THE ONE SHAPE THAT CANNOT BE RELEASED

`tools/audit_event_subscriptions.py` printed **"85 subscriptions in Runtime/, 0 with no matching
unsubscribe"** and had done for its whole life. Two independent blind spots, and the risk ordering
was exactly backwards in both.

**Blind spot 1: anonymous delegates.** `SUBSCRIBE` requires a NAMED handler, because keying the
pair on a name is what makes the `-=` lookup possible. An anonymous delegate has no name, matched
nothing, and **fell out of the count entirely**. ⚠️⚠️ **A named handler with no `-=` MIGHT leak; an
anonymous one provably cannot be released, because there is no reference to hand to `-=`.** So the
shape with the strongest guarantee of leaking had no coverage at all. That is
`audit_audio_reach.py`'s fault one file over (`CLAUDE.md` § 7.1: it *"LIED for its whole life"*).

**Blind spot 2: Unity's own events are camelCase.** The `PASCAL_TAIL` heuristic that tells a
subscription from an accumulator required BOTH sides to end in PascalCase, and
`SceneManager.sceneLoaded`, `InputSystem.onDeviceChange`, `InputSystem.onAfterUpdate` and
`Application.logMessageReceived` are not. ⚠️⚠️ **Those are engine statics that live for the whole
process, which is the worst possible publisher for a per-scene subscriber**, and every one was
skipped without being counted. Three real subscriptions in `MatchAbandon.cs` and `OutlineNormals.cs`
were never checked at all; both happen to release correctly, which is luck rather than coverage.

**The audit now reads 90 named subscriptions, 13 anonymous, 0 findings**, up from 85 and 0.
⚠️ **All thirteen anonymous sites were audited individually and all thirteen are safe**, each with
a written row in `ANONYMOUS_FOREVER` saying which of the two valid answers it uses: (a) subscriber
and publisher are both process-lifetime and the subscriber is constructed once, or (b) the
publisher is owned by the subscriber. The stale-row rule applies to that list too.

⚠️ **THE camelCase WIDENING WAS TOO LOOSE ON ITS FIRST TRY AND THE PascalCase RULE EARNED ITS
KEEP.** Accepting any dotted target readmitted `palm.y += HandTopLift` from `CharacterVisual`, a
vector accumulator with a PascalCase constant on the right, which is precisely the noise that rule
was written to stop. It is narrowed to targets whose final segment is **multi-word camelCase**: an
engine event name always carries an internal capital (`sceneLoaded`, `onAfterUpdate`), a component
like `.y` or `.magnitude` never does.

**Proved able to fail:** breaking one `ANONYMOUS_FOREVER` row produced both the finding and the
stale-row error.

### 150.7 ⚠️⚠️ OPEN, AND IT NEEDS AN EAR: THERE IS NO AUDIO LISTENER AT THE PLAYER

⚠️⚠️ **THE ONLY `AudioListener` IN THE GAME IS ON `~GameServices`, WHICH IS CREATED AT WORLD
ORIGIN AND NEVER MOVED, NEVER PARENTED AND NEVER ROTATED.** `AudioDirector.Awake` adds it, and
`KeepOneListener` actively **disables** any listener a scene brings, including one on an arena
camera. Nothing follows the camera and there is no `Update` that moves it.

**Meanwhile every pooled voice is fully 3D**: `spatialBlend = 1.0`, `AudioRolloffMode.Linear`,
`minDistance = 2.0`, `maxDistance = 32.0`. So the game computes distance and pan **from the arena's
origin rather than from the player's ears.**

**The arithmetic.** `Balance.ConfinementRadius` is 7.0, so a far corner of the danger zone is
`sqrt(7^2 + 7^2) = 9.9 m` from origin. Linear rolloff gives `1 - (9.9 - 2) / (32 - 2) = 0.74`.

| | |
|---|---|
| **Attenuation** | across the whole box, **1.00 at the centre to 0.74 at a corner**: audible, modest, and NOT the main cost |
| ⚠️⚠️ **Panning** | **this is the real one.** A 3D source pans by its direction from the listener's transform, and the listener is a fixed, unrotated object at the centre of the map. A player at `(-5, 0, 0)` hears a slipper land at `(+5, 0, 0)`, 10 m directly in front of them, panned **right**, because it is world `+X` of the origin. A cue behind them at `(-7, 0, 0)` pans **left**. Left/right and front/back are anchored to the WORLD and not to the head, so for every player except one standing exactly on the origin the stereo image is telling them something that is not true |

⚠️ **THIS IS THE SAME SHAPE AS § 150.2 ONE SYSTEM OVER**: a directional cue that is computed,
delivered, and points at nothing. `docs/VISION.md` § 0 is why it matters here specifically: the
whole tension is the run back in for your slipper, and hearing where the taya is coming from is
part of that read.

⚠️⚠️ **IT IS NOT FIXED IN THIS PASS, AND THE REASON IS A REAL COUPLING RATHER THAN CAUTION.**
Moving the listener to the camera **breaks every UI cue in the game on the same day**. Seven cues
are deliberately fired at `Vector3.zero` (`score_award`, `match_win`, `round_end`, `MenuSfx`, the
hitmarker), and today that works precisely BECAUSE the listener never moves: a cue at the origin is
a cue at the listener, so it plays centred at full volume. With a listener that follows the player,
those become 3D sounds sitting at world origin, attenuating and panning as the player walks.

**So the fix is two halves and the second one is the mandatory half:**

1. The listener follows the active camera (`CameraRig`, and the spectator rig, which is a fourth
   rig entirely, `CLAUDE.md` § 4).
2. **Non-diegetic cues get a 2D path**, either `spatialBlend = 0` on a dedicated UI voice or a
   `PlayUi(id)` entry point. ⚠️ Today `AudioDirector` has **no** non-positional API at all, which
   is a genuine strength (a 2D world cue is impossible by construction), so this must be added as
   an explicit, named, non-diegetic route rather than by relaxing the existing one.

⚠️⚠️ **AND THEN A HUMAN HAS TO HEAR IT.** `CLAUDE.md` § 6 makes sourced SFX provisional until 🧑
has heard them in play, and this changes the spatial image of **every** cue at once. **What needs
judging, specifically:** whether a slipper landing behind you now reads as behind you; whether the
taya's footsteps read as approaching; and whether the UI cues still sit flat and centred rather
than drifting. It is in [`../Attention.md`](../Attention.md).

### 150.8 ✅ THE SLIDE HAS ITS OWN ACTION NAME, WHICH CHANGES NOTHING TODAY

§ 146.6 wants a retrieval-slide clip and calls it art work. Both call sites (`CombatVerbs
.ReleaseSlide` and `.HostResolveSlide`) asked for `"lunge"` **by name**, so the day the clip landed
somebody would have had to find them among the four `PlayAction("lunge")` calls in that file and
know which two were the slide's.

They ask for `"slide"` now, and `CharacterAnimator` carries
`{ "slide", new[] { "slide", "attack-kick-right", ... } }`. ⚠️ **The fallback is byte-identical to
what `"lunge"` resolves to**: no `slide` clip exists on the CC0 rig, so `Play` walks past it to
`attack-kick-right` exactly as before. **This is a rename with a hook in it, not a behaviour
change**, and it is the same argument the `hero-*` chains already make. `ASTRA.md` task 3 is the
clip itself.

### 150.9 ⚠️ WHAT WAS AUDITED AND FOUND ALREADY CORRECT

⚠️ **§ 149's standing rule: a lead that turns out to be a false positive closes with the proof that
it is one, and is not written up as a bug that was fixed.**

| Brief item | Finding |
|---|---|
| **Practice sandbox teardown contract** (§ 149.6) | ✅ **Already correct and already tested.** `PracticeSandbox.Allowed` is `!NetAuthority.IsNetworked && !MatchAbandon.AuthorityRevoked`, the second clause being the existing canonical latch that covers the window where `IsListening` has gone false but the arena has not. `NationalsHardeningTests` already asserts both directions, including that a LOCAL quit does not revoke. **Nothing to do; documented and closed.** |
| **Duplicate / replay request sweep** (§ 149.4) | The vote paths were the most likely remaining class and **both are idempotent by construction**: `MatchResult.HostReceiveVote` is a SET keyed on peer id (`if (!_rematchVotes.Add(peerId)) return;`, commented *"idempotent, like the ready set"*), and `HostReceiveMapVote` is an assignment into a per-seat slot, bounds-checked on both seat and map. `OnReqResetMsg` bounds its phase byte and goes through `SenderOwnsClaimedSeat`. **All 13 GATING `tools/audit_*.py` pass with 0 findings**, including `audit_wire_payloads` (62 messages, 0 mismatched), `audit_wire_finite` (62 handlers, 0), `audit_request_call_sites` (60 entry points, 0 unreachable) and `audit_cue_relay` (48 sites, 0 ungated). ⚠️ **THIRTEEN, NOT FOURTEEN, AND THE FOURTEENTH IS NOT A GATE.** `qualify.py` keeps `audit_cue_audio.py` in `INFORMATIONAL_AUDITS` for the reason `f9ec3d6` gives: it measures PROVISIONAL assets and flags 6 of 117 cue files, mostly a DC offset on three UI clicks, every one pre-existing and waiting on 🧑's ear (`Attention.md` § 13). ⚠️ **This entry first said "all 14" because the loop that ran them read `tail`'s exit code instead of the audit's**, which is `CLAUDE.md` § 7's rule about asserting on the result and never on the exit code, caught one shell pipe over. |
| **Network hot-path lookups** (§ 149.5) | ⚠️ **Unchanged, and § 149.5's own survey is why.** The two paths the brief named first are already NOT scene searches: `MatchRpc.Unit(slot)` is a four-element list scan and `MatchRpc.SlipperFor` is a validated per-seat cache whose header records the fault it was written for. The remaining surface is the AI and the combat sweep, which is a different question, and § 149.5 records the specific reason not to build a `Slipper` registry in nationals week: the taya's parked tsinelas is `SetActive(false)` and the sixteen call sites split deliberately between `FindObjectsInactive.Exclude` and `Include`. **The row stays P2 and this pass did not touch it.** |
| **Audio coverage** | The system is stronger than the brief assumed in three of its four asks. `PlayAt`/`PlayAtVaried`/`PlayImpact` all REQUIRE a position and **there is no non-positional entry point at all**, so a stationary 2D world cue cannot be written; `PlayAtVaried` is the pitch-window path (18 call sites); `PlayImpact` is the two-layer path (6 sites); and `TryGetClip` exists for sounds that MOVE, with the LRT consist as its one caller and 🧑's *"make it feel like its getting farther"* as its reason. ⚠️ **The one real defect is § 150.7 and it is upstream of all of it.** |

### 150.10 ⚠️ WHAT IS STILL OPEN

- **§ 150.7**, the listener. It is the largest open item in this section and it needs the two-half
  fix and then an ear.
- **The jeepney (§ 144.8)** is **diagnosed but not fixed**, which is what that entry asked for.
  `JeepneyFinishProbe`, added by `e49bd2b` and never actually RUN, was run: all 17 materials are
  `glTF/PbrMetallicRoughness`, `HasProperty("_Metallic")` is **False on every one** (cause 1), and
  the scene holds **0 reflection probes** (cause 2). The probe now also prints the names the shader
  really declares, **`metallicFactor`** and **`roughnessFactor`**. ⚠️ Both causes have to be fixed
  before the fourth possibility can even be asked, and roughness is the inverse of smoothness.
- **The multi-seed bot sweep (§ 145.6)** was not run in this pass. It is 5 to 8 Unity launches an
  arm and the retrieval slide is what it has to be pointed at first.
- **A maximum-effects VFX stress measurement** was not taken. `MatchFrameRateProbe` and
  `HudPerformanceProbe` exist and are in the `capture` group; the brief's question is specifically
  about overlapping Hero Strike abilities, which neither drives.

---

## 149 · THE FRESH-AUDIT FOLLOW-UP: MOVEMENT BUDGET, RE-ADMISSION, AND THE ONE-SHOT REQUESTS ⚠️⚠️ IN PROGRESS, 2026-09-05, branch `main`

⚠️⚠️ **THIS SECTION IS THE BRIEF AS WELL AS THE RECORD, ON PURPOSE.** It was handed over mid
session and every item in it is either a confirmed competitive defect or a lead that has to be
proved or disproved with a reproduction rather than patched on suspicion. **The queue rows at the
top of this file point here; this is where the reasoning and the acceptance criteria live.**

⚠️ **THE STANDING RULE FOR EVERY ITEM BELOW: BUILD THE REPRODUCTION FIRST.** A suspicious-looking
guard that turns out to be safe is worth a test and a corrected comment, not a rewrite. A guard
that turns out to be exploitable is worth a deterministic regression that fails against the old
behaviour. Both outcomes get written down here.

⚠️⚠️ **LAST TSINELAS IS OUT OF SCOPE FOR THIS BATCH AND ITS HISTORY IS NOT COLLATERAL.** 🧑 was
explicit: this is an audit and hardening pass, not feature work. `LastTsinelasMatchHalfTests` and
`LastTsinelasDirector` stay exactly as they are; nothing here is a reason to delete either. § 128
still owns that feature's open half.

### 149.1 ⚠️⚠️ P0 CONFIRMED AND FIXED: PACKET FREQUENCY BOUGHT DISTANCE

**The lead.** `MatchRpc.AcceptMove` grants a movement allowance per PACKET:

```csharp
double dt = _lastAcceptedMoveAt.TryGetValue(slot, out double previous)
    ? Math.Max(0.0, Math.Min(2.0, now - previous))
    : Time.fixedDeltaTime;
float allowance = MoveBaseLeeway + MoveMaxMetresPerSecond * (float)dt;
```

`MoveBaseLeeway` is **0.85 m** and it is added to EVERY accepted packet, whatever the interval.
So the budget is not a rate, it is a rate plus a per-packet constant, and a client chooses how
many packets to send.

**The invariant this has to satisfy:** *packet frequency must not increase the total physical
distance a client can authoritatively travel over a given amount of authoritative elapsed time.*

**Required work:** trace the authoritative path on current `main`, build a deterministic
reproduction that sends N packets and 10N packets over the same server-elapsed time and compares
the total distance each is allowed, then fix with an elapsed-time budget rather than a packet-rate
limit. A rate limit answers "how often" and the question is "how far".

**Cases that have to be covered:** the expected cadence, a very high frequency, many tiny moves
followed by a big one, zero and near-zero distance packets, duplicates, rejected packets, a burst
after silence, delayed and reordered packets, round transitions, respawn and reposition, stun and
commitment, reconnect and seat transitions, and legitimate host corrections.

⚠️ **AND A REJECTED REQUEST MUST NOT REFRESH THE ALLOWANCE A LATER ONE SPENDS**, unless that is
explicitly intended and proven safe.

#### THE REPRODUCTION, BEFORE THE FIX

The old formula replayed over one second of server time, at the rates a client chooses:

| Submit rate | Per-packet allowance | Distance bought in one second | Arena widths |
|---|---|---|---|
| 50 Hz (the physics rate) | 0.85 + 28 x 0.020 = **1.410 m** | **70.5 m** | 5.0 |
| 60 Hz | 1.317 m | 79.0 m | 5.6 |
| 120 Hz | 1.083 m | 130.0 m | 9.3 |
| 500 Hz | 0.906 m | **453.0 m** | 32.4 |
| 1000 Hz | 0.878 m | 878.0 m | 62.7 |
| 5000 Hz | 0.856 m | **4278.0 m** | **305.6** |

**The arena is 14 m across.** Every individual step in the 5 kHz row is 0.856 m, which is well
inside a plausible physics step, so nothing in the old guard could see it. The client picks the row.

⚠️ **AND SILENCE WAS ITS OWN LEVER.** `Math.Min(2.0, now - previous)` bounded the RATE term and
nothing bounded the total, so **one packet after two seconds of quiet bought 56.85 m**, four arenas,
in a single step.

#### THE INVARIANT, AFTER THE FIX

> Over any interval of authoritative elapsed time T, the total distance a seat can have accepted is
> at most `MoveBudget.Ceiling + MoveBudget.MetresPerSecond x T`, **whatever the packet count**.

`Core.MoveBudget` is a balance of METRES that accrues with the host's own monotonic clock and is
spent by accepted movement. `MatchRpc.AcceptMove` debits it last, after the finite check, the arena
bounds and the velocity bound, so **a message refused for any other reason costs nothing** — which
is § 149.1's second requirement, and it falls out of the shape rather than needing its own rule.

⚠️⚠️ **IT IS NOT A PACKET-RATE LIMIT AND THAT WAS A DELIBERATE CHOICE.** A rate limit answers "how
often may you speak" and the question is "how far may you have gone": one tuned for 50 Hz refuses a
client that legitimately submits at 144, and a loose one still multiplies the budget by its own
slack.

| Constant | Value | Why |
|---|---|---|
| `MetresPerSecond` | **28.0** | unchanged. `Balance.LungeSpeed` is 7.746 m/s and that is the fastest impulse in the game, so this is 3.6x the fastest legitimate movement |
| `BurstMetres` | **0.85** | unchanged, and it is the term that used to renew per packet. One physics step of slack, spent and refilled by the clock rather than gifted per message |
| `CatchUpSeconds` | **1.0** | new, and it is what makes "a burst after silence" safe. The old equivalent was 2.0 s of RATE with no cap, worth 56.85 m; the ceiling is **28.85 m** now |

**Deterministic regression, `Core.Tests/MoveBudgetTests.cs`, 11 cases in 38 ms**: the same second at
50 / 500 / 5000 Hz buys the same distance and all three sit under the bound; a thousand tiny steps
bank nothing; a thousand REFUSALS leave a seat with exactly the credit a silent seat has; zero-
distance packets are free; duplicates are charged twice; silence banks at most the ceiling; a
one-second stall is still forgiven; NaN, both infinities and every negative are refused **and poison
nothing**; a backwards clock subtracts nothing; a forgotten seat starts the next owner on the burst
and not on the ceiling; and 500 honest steps at lunge speed are all accepted.

⚠️ **THE ADVERSARIAL CASES COST 40 ms BECAUSE THE RULE IS ENGINE-FREE.** The same sequences through
a real transport are two built players, a shaped link and fifteen minutes.

⚠️ **A HOST TELEPORT NEEDS NO RESET AND THAT IS MEASURED RATHER THAN ASSUMED.** After a respawn or
a round start the host has already moved its own copy, so the client's first stale packet is a big
delta and is refused once, costing nothing, and the correction it receives makes the next one small.
`Forget()` is called only where the CHAIR changes hands (`HostTakeSeatBackFromBot`, `HostPeerLeft`),
where the new owner must inherit neither a drained budget nor a bank.

### 149.2 ⚠️⚠️ P0 CONFIRMED AND FIXED: IT COULD, UNDER A TOKEN THE CLIENT CHOSE

**The lead.** The identity/hello message may be accepted again after admission has already
completed for that transport connection.

**Trace the whole path:** transport connection, identity, approval, the player/session record,
seat assignment, reconnect, spectator transitions, disconnect and seat hold, re-admission.

**Adversarial sequences to drive:** identity twice on one connection, repeatedly, an immediate
replay after a successful admission, a duplicated packet, the same logical player from a SECOND
connection while the first is live, a retry during join, during reconnect, during a spectator
transition, a stale identity during seat handover, and a stale identity after a newer session
already owns the seat.

**What to look for:** admission firing twice, duplicate session objects, duplicated callbacks or
subscriptions, duplicate spawning, player-count corruption, **duplicate scoreboard identity
(§ 141)**, seat reassignment, stale seat reclamation, taya corruption, state resurrection, two
transports claiming one logical player, and any score or state exploit that duplication opens.

**The invariant:** *initial transport identity admission happens once per live transport session.
Any valid retry is idempotent. Reconnect and reclamation follow their explicit state machine
rather than silently re-running first-admission behaviour.*

#### WHAT THE TRACE FOUND

`NetSession.ApproveConnection` reads the durable token out of the connection payload **once** and
stores it in `_helloByClient`; `OnClientConnected` seats the peer with it. The `Identify` RPC that
follows carried **its own copy of the token**, and `MatchRpc.HandleIdentify` passed that copy
straight to `LobbySession.Admit`, which was never told the two could differ.

`Admit` treats a matching token as a fast reconnect **by design**: it copies that record's seat,
spectator flag, picks and rating onto the sender and removes the matched record from `_peers`. So:

| Sequence | What happened |
|---|---|
| `Identify` with ANOTHER live peer's token | the sender takes the victim's chair. ⚠️ **The victim is not disconnected**, so its socket keeps submitting movement and verbs for a seat the lobby now says belongs to somebody else. Two transports, one logical player |
| `Identify` with a token nobody holds | no match in `_peers`, so `RuleOnArrival` runs afresh: with a free seat the sender **moves itself to a different chair** while `MatchInstaller` still drives its old body, and with none it turns itself into a **spectator mid-match** |
| `Identify` repeatedly | the whole arrival fan-out re-ran every time: three ClientRpcs, the ready tally, the lobby picks, the seat picks **and a full world snapshot**, all broadcast to every peer, plus a live account-endpoint call through `VerifyArrivalAsync`. A client can send this message as fast as it likes |

⚠️⚠️ **HONEST ABOUT REACH.** The first row needs the victim's durable token, and that token **never
crosses the wire** (`LobbySeatInfo` does not carry it and nothing in `MatchRpc` writes it), so it is
a shared-machine or leaked-`settings.json` attack rather than a remote one. **The second and third
rows need nothing but the ability to send the message the client already sends**, and the invariant
break underneath all three is not conditional on anything.

#### WHAT SHIPPED

1. **The token is the one this host approved, never the one in the message.**
   `NetSession.ApprovedTokenFor(clientId)` answers from `_helloByClient`, and answers the host's own
   token for the local client. ⚠️ It falls back to the message's token only when this host approved
   no connection for that id, which is the solo and LAN-host path where `IdentifyServerRpc` calls
   straight through with `peerId` 0 and there was no approval step to pin anything.
2. **Admission is once per transport session and a repeat is a RETRY.** `_identified` is a set of
   peer ids, cleared by `HostPeerLeft` because a transport that left and came back is a new session.
   A repeat still updates the picks and the authorised cosmetics and still re-sends the SENDER its
   own mode, map, difficulty and seating, which is exactly what a peer resending because it thinks
   the first was lost needs. **The four room-wide broadcasts and the endpoint call are first-time
   only.**
3. **`HostLateJoin` still runs on every identify** and must keep doing so: its own header records
   that gating it left the host's AI driving a chair a re-identifying player had already taken.

⚠️ **`_identified` IS ITS OWN SET AND NOT `_spawned`.** That one means "has been sent the world
snapshot", and its header records what happened the last time somebody reused it for a second
question. One set, one meaning.

### 149.3 ✅ P1 CONFIRMED AND FIXED: THE BUFFER-SKIP VOTE HAD A JOIN RACE

`NetworkManager.IsListening` becomes true before a client has finished approval, and this
repository's own networking notes say so. The other one-shot request paths use the stronger
connected-client condition; the buffer-skip vote appears to gate on `IsListening`. A player who
presses skip inside the join window has the press consumed with no route to deliver it.

**Done looks like:** the same readiness invariant the other one-shot paths use, a focused
regression over not-listening / listening-but-not-admitted / connected / host / client /
teardown, and a sweep of the neighbouring one-shot methods for the same copy/paste.

⚠️ **DO NOT MOVE LONG-LIVED REPLICATION PATHS ONTO THE STRONGER GATE.** A snapshot stream and a
one-shot command are different problems, and tightening the first would break a listen host.

#### CONFIRMED, AND IT IS A COPY OF THE NOTE RATHER THAN OF THE CODE

`MatchRpc.RequestSkipBufferServerRpc`'s own summary said the right thing, in as many words:

> *"It returns whether the vote reached the wire, like `DeclareReadyServerRpc` and for the same
> reason: `IsListening` is true from `StartClient` and not from approval, so a press made during the
> join window has nowhere to go and the caller has to know that rather than believe it voted."*

and the line under it read `if (_nm == null || !_nm.IsListening || ...) return false;`. So the
method returned **true** for a message that went nowhere, and `BufferSkipVote` cleared
`_sendPending` believing it had voted. The retry machinery was already correct and was never
reached.

**Fixed to `!_nm.IsConnectedClient`.**

#### THE SWEEP OF THE NEIGHBOURS

Every client-to-host request that RETURNS whether it was delivered, which is the whole class where
a silently consumed press is possible:

| Method | Gate | |
|---|---|---|
| `DeclareReadyServerRpc` | `IsConnectedClient` | correct already |
| `SendChatServerRpc` | `IsConnectedClient` | correct already |
| `VoteRematchServerRpc` | `IsConnectedClient` | correct already |
| `SelectMapVoteServerRpc` | `IsConnectedClient` | correct already |
| `RequestSkipBufferServerRpc` | ~~`IsListening`~~ -> `IsConnectedClient` | **the one** |

⚠️ **THE VOID-RETURNING REQUESTS WERE LEFT ALONE, DELIBERATELY.** `ReqPunch`, `ReqLunge`,
`ReqSlide`, `ReqShove`, `ReqGrab`, `ReqThrow` and the rest check only that the transport exists,
and that is right for them: they carry no "it was delivered" claim to be wrong about, and a
gameplay verb pressed during the join window is a verb pressed before the player has a body.

### 149.4 ⚠️ P1: THE ONE-SHOT / DUPLICATE / REPLAYABLE REQUEST SWEEP

Concrete classes to look for on the competitive paths, in order of what would actually cost a
match: a rejected packet refreshing a timer, a duplicate RPC granting an effect twice, a stale
RPC applying after ownership changed, an old session's packet reaching a new session, repeated
ready/start/rematch, repeated spectator transitions, a repeated reconnect claim, a duplicate
throw, grab, shove, punch, lunge or retrieval slide, a cooldown or resource deducted twice,
prediction rollback that does not match the authoritative refusal, and a missing sequence number
or session epoch where one is genuinely needed.

⚠️ **NO GENERALISED FRAMEWORK.** Concrete reachable bugs with focused tests, and nothing bigger
unless the architecture actually demands it.

### 149.5 ⚠️ P2: REPEATED SCENE LOOKUPS IN NETWORK HOT PATHS

Seat to `CharacterMotor`, seat or origin to `Slipper`, and the current `Lata` are resolved by
runtime discovery in replicated and request-processing code. `MatchRpc.SlipperFor` already
learned this once and carries the note.

⚠️⚠️ **MEASURE FIRST.** Identify the exact APIs and the real call frequency, decide whether the
path is per packet, per snapshot or per frame, and instrument representative four-player traffic.
If it is material, add the smallest safe match-local registry owned by an existing lifecycle
owner, with no stale reference after teardown, no static leaking into a second match, correct
behaviour across seat handover and reconnect, and tests over the registry's lifecycle. **If the
searches are negligible, record the number and leave the stable code alone.**

#### THE CALL-SITE SURVEY, 2026-09-05

`grep -rn "FindObjectsByType\|FindFirstObjectByType\|FindAnyObjectByType" Assets/TumbangPreso/Runtime`
is **134 lines**, of which the ones on a per-frame or per-decision path are:

| Where | Sites | Frequency |
|---|---|---|
| `AIController` | **12** `FindObjectsByType<Slipper>` | per bot per decision, four bots |
| `CombatVerbs` | 2 | one per `Verb.Lunge` press (an edge), one **per frame while a slide is live** (0.34 s) |
| `Carrier` | 1 | per pickup attempt |
| the four hero kits and `HeroHazards` | 4 | per cast |
| `SpectatorCamera`, `SpectatorInterest` | 3 | ⚠️ `SpectatorInterest` already caches, with the note *"rather than every frame"* |

⚠️⚠️ **AND THE TWO THE BRIEF NAMED FIRST ARE ALREADY NOT SCENE SEARCHES.** `MatchRpc.Unit(slot)`
goes to `RoundDirector.PlayerAt`, which is **a four-element list scan**, and `MatchRpc.SlipperFor`
is a validated per-seat cache whose header already records the fault it was written for (*"a host
paid four scene-wide type scans and four fresh arrays fifty times a second"*). **The request and
snapshot paths the row was worried about are clean.**

**So the remaining surface is the AI and the combat sweep, not the network layer**, and that is a
different question from the one this row was opened with.

⚠️⚠️ **NOT REFACTORED THIS SESSION, ON PURPOSE, AND THE REASON IS SPECIFIC RATHER THAN CAUTION.**
The obvious fix is a `Slipper` registry maintained by `OnEnable`/`OnDisable`, and the taya's parked
tsinelas is `SetActive(false)`: call sites split between `FindObjectsInactive.Exclude` (most) and
`Include` (`NetStateReport.FindSlipper`, deliberately, because *"switched off is exactly the state
this report has to be able to show"*). A registry that answered one of those two questions for all
sixteen call sites would silently change which slippers a bot, a cast or a diagnostic can see, and
**the taya's shoe is the object this game is most careful about** (§ 78.1's `OwnerSlot` fault is
exactly that object read through the wrong index). That is a change worth making with a measurement
in front of it and a day that is not the week of the nationals.

**Done looks like:** a PlayMode measurement of `FindObjectsByType<Slipper>` cost against the real
arena's component count, and either a registry with both visibilities modelled explicitly, or the
number written down and the code left alone. The row stays P2.

---


### 149.6 ✅ P2 MEASURED: THE COMMENT WAS FALSE, AND THE PREDICATE IS CORRECTED ANYWAY

A comment claims the sandbox stays denied through disconnect teardown because the session is
still networked, while `NetSession.IsNetworked` is tied to `NetworkManager.IsListening`, which
goes false during teardown. Either the comment is stale or there is a reachable window.

**Both outcomes are work.** If authority revocation already blocks gameplay first, correct the
comment and add a lifecycle test so nothing later relies on the false assumption. If practice
behaviour can actually switch on mid-teardown, fix the predicate, and do it by reading the
existing canonical latch rather than by keeping a second copy of authority state.

#### THE ANSWER IS A, AND THE PREDICATE IS FIXED ANYWAY

**The claim, in `PracticeSandbox`'s own header:** *"`!NetAuthority.IsNetworked`, which is false for
a host, false for a client, false in a Relay match and false in a LAN one — **including the frame a
session is being torn down, because the provider is what answers**."*

**The provider:** `NetSession.IsNetworked => _nm != null && _nm.IsListening`. `IsListening` goes
FALSE the moment `Shutdown()` runs, and `NetSession.IsHost`'s own note in `MatchAbandon` records the
same fact from the other side: *"`_nm == null || !_nm.IsListening || _nm.IsServer`, so a CLIENT
whose transport has just stopped satisfies the middle clause"*. **So the clause is false**: through
a teardown this predicate says "offline" while the arena, the bodies and the ability systems are
all still on screen.

**Why nothing could reach it, which is not the same as it being true:**

| | |
|---|---|
| `Toggle()` | `Wanted = !Wanted && Allowed`, so the switch **cannot be armed while networked** at all |
| `GameLaunch.Reset` | calls `PracticeSandbox.Clear()` on the way into every match, so `Wanted` is false entering a networked one |
| `MatchAbandon.AuthorityRevoked` | § 143.9's latch stops `ShouldResolve()` the moment a host is lost, so nothing authoritative resolves in that window regardless |
| `TournamentGuard` | names `PracticeSandbox.Wanted` as a tournament modifier and clears it before a bracket match |

⚠️⚠️ **THE COMMENT IS THE DEFECT AND IT IS A REAL ONE.** `TournamentPreset.Modifiers` quotes this
guard by name as one of the eight things a bracket match depends on, so its written reason is load
bearing: **a guard whose stated reason is wrong is a guard the next person builds on.** That is
`docs/TODO.md` § 92.7's fault (a note crediting the wrong fix) and `CLAUDE.md` § 5's drift rule.

**Corrected in place rather than deleted**, with the false clause quoted, and the predicate is
`!NetAuthority.IsNetworked && !MatchAbandon.AuthorityRevoked` now. ⚠️ **That is the EXISTING
canonical latch and not a second copy of authority state**, which is what the brief asked for: it
is set by `MatchAbandon.Note` on host loss, removal, a version refusal and a full lobby, and
cleared by `MatchAbandon.Forget` when the next match begins, so the sandbox is denied for exactly
the window in which "offline" is a lie. ⚠️ A LOCAL quit deliberately does not revoke and does not
need to: a player who pressed QUIT really is offline afterwards, and denying them their own solo
sandbox would be the fix causing the bug.

### 149.7 ⚠️ P2: THE TEST SUITE'S OWN VALUE

**The goal is not fewer tests.** It is removing assertions that carry no unique failure signal:
source-text assertions that duplicate behavioural coverage, tests that regex a constant out of a
file and then assert the same compiled constant, assertions about comments rather than behaviour,
historical hardening tests whose failure a stronger central invariant has since made impossible,
and expensive Unity probes that only repeat something cheaper coverage already proves.

⚠️⚠️ **THE RULE: FOR EVERY TEST REMOVED OR MERGED, NAME THE STRONGER TEST THAT NOW OWNS THAT
INVARIANT.** Never delete a failing test to green the suite, never trade coverage for a faster
qualification, and never drop an integration test because a unit test looks similar without first
checking whether the lifecycle or the network behaviour differs.

#### THE ONE THE BRIEF NAMED, REMOVED, 2026-09-05

`LastTsinelasMatchHalfTests.TheEliminationTravelsAndThePeersAgreeOnTheStockTable` asserted one
claim twice in one method:

```csharp
var moved = Regex.Match(session, @"ProtocolVersion\s*=\s*(\d+)");
Assert.IsTrue(moved.Success, "NetSession.ProtocolVersion is gone or renamed.");
Assert.GreaterOrEqual(int.Parse(moved.Groups[1].Value), 22, ...);
// ⚠️ THE SECOND STATEMENT OF THE SAME CLAIM ... so it stays
Assert.GreaterOrEqual(Net.NetSession.ProtocolVersion, 22, ...);
```

**The stronger test that owns it now is the compiler.** The only thing the regex added over the
compiled assertion was *"ProtocolVersion is gone or renamed"*, and
`Net.NetSession.ProtocolVersion` on the next line **does not compile** if that happens: a build
failure rather than a test failure, and one no `-testFilter` can skip. A source assertion that is
strictly weaker than the compiler is a second place to edit when somebody moves the number, and
nothing else.

⚠️ **THE LOOSE `>= 22` STAYS LOOSE.** `ChatAndLobbyChromeTests.TheProtocolCarriesEveryRosterBump`
owns the EXACT value with a paragraph per bump; this one owns *"the number had already moved past
21 by the time this feature shipped"*, and pinning it would go red every time somebody correctly
bumped a shared constant. Two different questions, one each.

#### WHAT WAS LOOKED AT AND DELIBERATELY KEPT

⚠️⚠️ **NOT EVERY SOURCE-TEXT ASSERTION IS A DUPLICATE, AND THE DIFFERENCE IS WHETHER THE COMPILER
COULD HAVE ANSWERED.** Three kinds were checked and only the first is redundant:

| Kind | Verdict |
|---|---|
| A constant read out of source AND asserted as a compiled value | **Redundant.** The compiler proves the name and the value harder than the regex does |
| A CROSS-LANGUAGE contract (`WorkingTreeRules` in C# and `qualify.py` in Python; `IntegrityRules.Digest` and `match-record.js`) | **Kept.** Nothing compiles both sides, which is precisely why `tools/check_digest_contract.js` exists |
| A CALL-SITE claim (*"every screen backs out through `MenuNav`"*, *"both boards ask `SeatLabel`"*, *"`HandleIdentify` pins the approved token"*) | **Kept.** The compiler cannot see that something is NOT called, and `CLAUDE.md` § 4a records three faults that were invisible to every other check |

⚠️ **AND THE EXPENSIVE PROBES WERE NOT TOUCHED.** `InputSurfaceProbe`, `BotBehaviourProbe` and
`AbilityShowcaseProbe` are minutes each and every one of them tests integration, lifecycle or a
picture that no cheaper coverage reaches. § 149.7's own rule says an integration test is not
replaced by a unit test that looks similar.

**Still open:** the rest of the sweep. This closed the case the brief named and set the rule for
the next one; nobody has walked the whole suite.

### 149.8 ⚠️⚠️ P1 CONFIRMED AND FIXED: THE SINGLE EXIT FROM A MATCH CLEARED NOTHING

The remaining lifecycle risk is not the first launch, it is process-wide state surviving into
another match: host, play, results, leave, then create or join a DIFFERENT match without
restarting the player.

Audit `GameLaunch`, `SceneFlow`, the preview flags, the practice and debug switches, spectator
state, replay state, the match installer's registries, static events, cached seat identity,
authority revocation, format, mode, the tournament preset, bot fill, and every
`DontDestroyOnLoad` object.

**Test two sequential matches with meaningfully different settings**: Hero Strike with bots and a
spectator, then Classic with a different seat composition and a real player. Nothing from the
first may leak into the second: not the mode, the format, the bot state, a preview or practice
switch, seat ownership, the taya, a score, a cooldown, a replay marker, an event subscription or
a network role.

#### WHAT THE AUDIT FOUND

**`GameLaunch.Reset()` had exactly one caller: `SceneFlow.StartTraining`.** So
`SceneFlow.LeaveMatchToMainMenu`, whose own header calls it *"the one way out of a match, and the
only one that ends the session as well as the scene"* and which the pause panel, the results board
and both result screens all come through, cleared **none** of the launch block.

⚠️⚠️ **THE FIELD WITH TEETH IS `GameLaunch.Spectator`.** `MatchInstaller.HumanSeat` answers **-1**
while it is set, and `MatchInstaller` reads that to decide who gets a `PlayerInputReader` and which
seats get an `AIController`. So a player who spectated one match and then started a solo one got an
arena in which **nobody was driving their seat**. `ConvertedMatchSetup` clears it on the way into
the lobby, which covers the lobby route and not the ones that skip it.

**Fixed by calling `GameLaunch.Reset()` from the single exit**, which is safe precisely because of
what that method is: `PendingAction` and `PendingJoinAddress` have already been consumed by the
arena being left, and `SeatTokens` is a reconnect claim on a match the player is walking out of.

#### WHAT WAS CHECKED AND IS ALREADY CORRECT

| State | Where it is cleared | |
|---|---|---|
| `MatchAbandon.AuthorityRevoked` | `NetSession.ConfigureTimeouts`, which *"every start path and `Stop` reach"* | ✅ and now asserted, because a latch surviving into a second match is a peer that cannot resolve anything in it |
| `GameLaunch.GuidedTutorial` | `GuidedTraining.OnDestroy`, so a scene unload clears it however the player left | ✅ and `Reset()` is now a second belt |
| `MatchInstaller.PreviewOnly` | `MapPreviewSurface` sets it true and false in a matched pair | ✅ |
| `PracticeSandbox.Wanted` | `GameLaunch.Reset()`, and `Toggle` cannot arm it while networked | ✅, and now reached from the exit as well |
| `SceneFlow.SelectedRules` | pinned by `TournamentGuard.Apply` and NOT persisted, deliberately (§ 143.18) | ✅ |

⚠️⚠️ **`GameLaunch.AllBots` IS DELIBERATELY NOT IN `Reset()` AND MUST NOT BE ADDED.** It is written
by `-tp-allbots` on the command line and belongs to the PROCESS rather than to a match: a harness
that asked for a driven session expects the second match to be driven too, and clearing it on every
exit would make `tools/net_matrix.py` and `tools/referee_run.py` measure three parked bodies from
their second match onwards. `TournamentGuard` is what clears it for a bracket match, which is the
one place it must not be set. **The test asserts that it survives**, so somebody "tidying" it into
`Reset()` fails rather than silently breaking every multi-match harness.

⚠️ **WHAT IS STILL OPEN IS THE TWO-MATCH INTEGRATION RUN ITSELF**, host/play/results/leave then a
different match in one process. The state audit above is the cheap half and it found the defect;
the expensive half is a PlayMode probe and is what the row in the queue still points at.

### 149.9 ⚠️⚠️ P1 CONFIRMED AND FIXED: A SPECTATOR COULD SET `Time.timeScale` TO NaN

Every client-supplied float, vector and rotation used for authoritative validation has to be
proved hostile-safe: NaN, both infinities, absurd but finite coordinates, an unnormalisable
direction, a zero vector where normalisation is assumed, and invalid angles.

⚠️⚠️ **A RANGE COMPARISON DOES NOT REJECT NaN.** In C# every ordinary comparison against NaN is
false, so `if (x > limit) return false;` PASSES a NaN through, and that is the shape most of these
guards are written in.

Paths to inspect by name: movement, throw, the retrieval slide, grab, punch, shove, pose updates,
and any spectator position that reaches an authoritative decision.

#### THE ONE THAT WAS REACHABLE

`MatchRpc.HostSetTimeScale` clamped the requested scale to 0..1 and assigned it to
`Time.timeScale`. `Mathf.Clamp` is exactly `if (v < min) v = min; else if (v > max) v = max;
return v;`, so **both infinities were handled correctly (they DO compare) and NaN fell through both
branches unchanged**. `OnReqTimeMsg` checked that the sender was a spectator and never checked the
NUMBER, and `docs/TODO.md` records the ruling that put that button there: 🧑, *"give spectators the
authority to pause, all of them can pause"*. So **any spectator in a live match could freeze the
host and, through the `SyncTime` broadcast, every peer.**

**Refused rather than clamped to a guess.** A NaN is a malformed request, not a big number to bring
into range, and picking 1.0 for it would un-pause a match somebody had deliberately paused.

#### THE SWEEP, AND WHAT ELSE GOT A GUARD

Every client-to-host request path already checked `Finite` before it clamped, which is the ordering
that matters: `PlausibleIntentPose` opens with it, and the lunge's `power` and the throw's `charge`
and `spin` are finite-checked before `Mathf.Clamp` sees them. `OnSubmitMoveMsg` delegates to
`AcceptMove`, which validates all three of its fields and then spends `Core.MoveBudget`, which
refuses NaN a second time by construction.

The HOST-to-client direction was unguarded throughout, and that is a robustness fault rather than an
integrity one, but a cheap and visible one: **a non-finite `Vector3` or `Quaternion` assigned to a
`Transform` is REFUSED by Unity, logged once per frame, and the object is left wherever it last
was.** A tsinelas that stops replicating, with nothing in the log but a repeating engine warning, is
a venue failure that reads as "the game broke". Guards went onto `OnSyncTimeMsg`, `OnSyncUnitMsg`
(pose and the stamina/stun floats), `OnPlayAbilityMsg`, `OnSyncAbilityMsg`, `OnSyncWorldMsg`,
`OnSyncLataMsg`, `OnSyncSlipperMsg`, `OnLataPoseMsg` and `OnSlipperPoseMsg`.

⚠️ **A NaN IN A BAR OR A COOLDOWN IS SILENT AND PERMANENT.** Every `>= 0` against it is false, so a
NaN stamina is a body that can never spend and never regenerate, and a NaN cooldown is an ability
that reads READY for ever on that peer while the host refuses every cast.

#### THE AUDIT THAT KEEPS IT

`tools/audit_wire_finite.py` is new and gating. It discovers every named-message handler by
signature (rather than from a list, for `InputSurfaceProbe`'s reason) and requires every
`ReadValueSafe(out float/double/Vector3/Quaternion/Vector2 ...)` to be reached by a `Finite(...)`
call on that variable, by `PlausibleIntentPose`, or by a **named delegate that validates on its
behalf** (`AcceptMove`, `PlausibleIntentPose`, `HostSetTimeScale`), each with its reason written
beside it. **Current reading: 62 wire handlers, 62 numeric fields, 0 findings**, and it was proved
able to fail by deleting one guard.

⚠️ **A `Quaternion` OVERLOAD OF `Finite` DID NOT EXIST AND NOW DOES.** A rotation needs the check as
much as a position and there was no way to write it.

### 149.11 ⚠️⚠️ FOUND WHILE VERIFYING: EVERY UNITY RUN DIRTIED A TRACKED FILE, SO THE GATE COULD NEVER PASS

Not on the brief, found by running it. **A plain `-batchmode -runTests -testPlatform EditMode`
launch rewrites `ProjectSettings/QualitySettings.asset`**, moving Ultra's `antiAliasing` from 4 to
0. Measured twice in a row on `837eb0a` from a clean checkout.

That was cosmetic until § 145.1 made a dirty tree a refusal. It is not cosmetic now: **the
qualification rewrites a tracked file while it runs, so the report stage reads a dirty tree and the
verdict is NOT QUALIFIED with every test green.** A gate that fails because it ran is worse than no
gate, because the first thing anybody does with it is pass a flag.

⚠️⚠️ **AND `AntiAliasModes`'S OWN NOTE SAID THIS COULD NOT HAPPEN.** *"MEASURED RATHER THAN
ASSUMED, 2026-09-03: a full batchmode PlayMode suite ... left it completely clean. The write-through
the header warns about is an INTERACTIVE editor behaviour."* It is not interactive: `GameSettings
.Apply` calls `AntiAliasModes.Apply` at boot in batch mode as well, and in the editor
`QualitySettings.antiAliasing` **is** the serialized asset, so writing the live value writes the
file. The note is corrected in place rather than deleted, because a falsified measurement is the
clearest possible warning about quoting one without re-running it.

**`QualityLevelStamp` regenerates the asset from `AntiAliasModes.QualityLevelSamples`** at editor
load, at editor quit (which fires in `-batchmode`) and on leaving play mode. It writes only the
`antiAliasing` field and only when it differs. That is `GameBuilder.ConfigureSplash`'s and
`ShaderWarmupCollection`'s shape and `CLAUDE.md` § 6.4's rule: **both places or neither**, resolved
by making one of the two generate the other.

⚠️ **`QualitySettingsAssetTests` IS KEPT AND IS NOT VACUOUS.** It still catches a hand edit the
stamp has not yet corrected, and it is the thing that would notice if the stamp stopped running.

### 149.10 ✅ THE ACCEPTANCE BAR FOR THIS SECTION

Each numbered item above closes only when it carries, in its own subsection: what was measured,
whether it was real, the reproduction, the invariant now in force, and the test that would fail if
somebody undid it. **A lead that turned out to be a false positive closes with the proof that it
is one and the test that keeps it that way**, and is not written up as a bug that was fixed.

---

## 148 · THE 2026-09-05 NATIONALS BATCH ✅ CLOSED, and archived in the commit that wrote it

⚠️ **A SESSION REPORT IS NOT AN OPEN ITEM** (`CLAUDE.md` § 2.3). This is the index row; the
reasoning lives in §§ 134.12, 141.7, 143.9, 143.15b, 144.7, 145, 146 and 147, each of which keeps
its own measurement.

**What moved:**

| § | What |
|---|---|
| 143.9 | Host loss is one outcome on every peer. `SessionEndRules`, `MatchAbandon`, and `ShouldResolve` no longer handing a client authority the instant its transport stops |
| 144.7 | The seat handover's rating travels. `ProtocolVersion` **23 to 24** |
| 134.12 | The replay capture is asynchronous, bounded, pooled and measured: **0.734 ms to 0.035 ms per call**, worst call **2.066 to 0.158** |
| 141.7 | The invariant model can express both ownership faults, and peer agreement compares seats |
| 143.15b | The cold start asserts a round RAN, and runs on this machine |
| 145.6 | The bot sweep can compare two arms against the noise floor, with a config digest |
| 145.1-145.4 | Four gates that could still come out green while proving less than they printed |
| 146 | The attacker's committed retrieval slide, on a control that did nothing |
| 147 | Structured highlight markers, joined to the replay, awarding nothing |
| FUTURE 12.1 | Map mechanical identity, written as a future item and not started |

⚠️⚠️ **THREE THINGS WENT WRONG IN THIS PASS AND EACH IS WRITTEN WHERE IT HAPPENED**, because they
are the parts that generalise:

1. **§ 146.4c: a test that waits in frames measures the machine's frame rate.** Sixty frames is
   0.03 s in batch mode. It read as the feature being broken for two runs, and what found it was
   a failure message carrying numbers.
2. **§ 134.12: a warm-up that spun frames warmed the renderer and not the thing under test.** The
   first `AsyncGPUReadback` request cost 147.7 ms once and stood as the path's "worst call".
3. **§ 146.4b: `tools/audit_presentation_reach.py` caught a double award on the run it was
   written**, which is the whole argument for having audits that read the source as text.

**Verification on `eae4e96` + this batch, on the Mac** (`CLAUDE.md` § 7): EditMode **409 cases,
408 passed**; PlayMode `match` **81/81**, `capture` **10/10**, the eight retrieval-slide cases
**8/8**, `ReplayCaptureProbe` green; **all 8 editor checks**; every source audit clean except
`audit_cue_audio.py` (the six `ui_*` DC offsets, `Attention.md` § 17.3) and, before it was fixed,
`audit_presentation_reach.py`. ⚠️ **`dotnet test Core.Tests` could not be run here at all**, which
is a machine fact (§ 145.5) and not a skipped step: the rules this batch added are asserted in the
EditMode suite for exactly that reason.

⚠️ **AND ONE THING THAT DID NOT MOVE AND SHOULD BE LEFT ALONE:** every `-batchmode` run on the Mac
editor rewrites `ProjectSettings/QualitySettings.asset` (Ultra's `antiAliasing` 4 to 0) and
`QualitySettingsAssetTests` fails on the run that caused it. § 142 already records it and the
remedy: `git checkout -- ProjectSettings/QualitySettings.asset`, and do not chase the red.

---

## 147 · THE GAME NOTICES ITS OWN GOOD MOMENTS AND WRITES THEM DOWN ⚠️ IN PROGRESS, 2026-09-05, branch `main`

**Classic already recognises skilled play and throws it away.** `Hud.ReportStyle` names a bank, a
curve, a snatch and a block while they are happening, pays a cosmetic bar, and then it is gone;
`MatchFlair` replicates sixteen kinds of event to every peer and each one draws a popup and
forgets it. **Nothing in the game could answer "what happened in this match, and when."**

⚠️⚠️ **THAT ABSENCE HAS ALREADY COST THIS REPOSITORY A FEATURE ONCE.**
`SpectatorCamera.QueueHighlight`'s own note: *"nothing in the buffer knew WHEN the tag was, so the
clip was still the last five and a half seconds whenever the key happened to be pressed. The title
and the footage were two independent claims."* That was fixed inside the replay ring by stamping
the marker onto the captured frame, and the fix expires with the frame: **ten seconds of pixels.**

### 147.1 ✅ WHAT LANDED

- **`Packages/com.tumbangpreso.core/Runtime/Highlights.cs`**, engine-free. `HighlightKind` (ten
  values, every one establishable from state the game already holds), `HighlightMarker` (kind,
  match time, round, actor, subject, one measurement, importance), `HighlightRules` (thresholds,
  the dedupe rule, the caption) and `HighlightLog` (a bounded ring with the dedupe inside `Add`).
- **Every threshold is a number the game already had an argument for**, which is the half worth
  reading: a close call is `Balance.LungeTagRadius`, because that is the distance at which the
  taya's dash actually catches somebody; a long knockdown is `Balance.ConfinementRadius`, because
  that is the edge of the danger zone; last-second is `Balance.ChargeFullTime`, because inside it
  nothing started now can still land; an evasion window is `Balance.TagStunTime`, because that is
  what one mistake costs.
- **`Diagnostics.MatchHighlights`** records from `MatchFlair.Play`, which runs on **every peer**
  with the same event data. That is one line and no new bytes on the wire: recording host-side and
  broadcasting would be a message carrying something every machine already knows.
- **`Diagnostics.HighlightWatch`**, installed by `MatchInstaller` with the arena, for the two
  kinds that are the ABSENCE of something. A close call is a tag that did not happen and has no
  call site.
- **The replay join**: `SpectatorCamera.LastClipMarker` reports the most important marker inside
  the window a clip actually covers. § 147's brief says the first useful version is *"gameplay
  event -> structured marker -> replay can identify that time window"* and explicitly not a
  broadcast director, so that is exactly what it is.

⚠️⚠️ **IT CHANGES NO SCORE AND A TEST READS THE SOURCE TO PROVE IT.**
`NationalsHardeningTests.NothingInTheHighlightLayerCanAwardAPoint` fails if either file names
`AddScore` or `ReportStyle`, and if the core file names `UnityEngine`. ⚠️ **It strips comments
first**, which it did not on the first run and which is `tools/audit_audio_reach.py`'s fault
exactly: `CLAUDE.md` § 7.1 records that audit lying for its whole life because it read a header
explaining a gate as a gate.

### 147.2 ⚠️ THE DEDUPE RULE IS THE PART WITH TEETH

One gameplay event reaches this layer several times. A knockdown arrives from `MatchFlair
.LataDown`, from the score watcher and, on a bank, from the bank detector. `SpectatorCamera`'s own
header records what that cost when nothing folded them: *"a knockdown, a tag and a sabotage are
three separate triggers, and `PollHighlights` adds a fourth"*, which produced the replay spam 🧑
reported and got a whole trigger deleted rather than fixed.

`HighlightRules.SameEventSeconds` is **1.5 s**, and it is bounded from both sides rather than
picked: longer than any chain one gameplay event produces (those arrive inside one physics step,
or one 5 Hz snapshot at the outside) and no longer than `Balance.LungeCooldown`, so **no genuine
second event can be swallowed**, the taya cannot tag twice inside it. Asserted both ways.

### 147.3 ⚠️ OPEN: NOTHING DRAWS IT YET

The markers exist, are deterministic, are deduplicated and are joined to a replay clip. **No
screen reads them.** The obvious next users, in order of how cheap they are:

1. **The end-of-match board.** Three lines under the scoreboard, off `HighlightLog.Report()`.
   ⚠️ `CLAUDE.md` § 6.2 first: what is the ONE thing on that screen, and is this it.
2. **A spectator ticker.** `SpectatorCamera` already draws an overlay and already knows the
   captions.
3. **An export.** `LastClipMarker` plus the clip is enough to name a file.

⚠️ **A CLIENT'S LOG IS ONE KIND SHORT AND THAT IS WRITTEN DOWN RATHER THAN FIXED.** The retrieval
is recorded in `Slipper.HostGrab`, which is host-side; every other producer rides `MatchFlair` and
reaches every peer. Adding a wire message for a caption would cost a protocol field for something
no rule reads. `MatchHighlights`' class note carries the argument.

---

## 146 · CLASSIC'S DEPTH COMES FROM MOVEMENT: THE COMMITTED RETRIEVAL SLIDE ⚠️ IN PROGRESS, 2026-09-05, branch `main`

`docs/VISION.md` § 1.1: *"CLASSIC IS NOT 'HERO STRIKE WITH THE POWERS TURNED OFF'. Do not add a
power to it ... Give Classic its own depth; do not give it powers."* And § 0: **the tension is the
retrieval, not the throw.**

**So the decision this adds is one sentence long:** *I can walk up and pick this up safely, or I
can commit and get there a third of a second sooner.*

### 146.1 ✅ IT ADDS NO BUTTON, AND THE ONE IT USES WAS DEAD

⚠️⚠️ **`Verb.Lunge` DID NOTHING AT ALL FOR THREE OF THE FOUR PLAYERS IN EVERY ROUND.**
`CombatVerbs.Update` reaches `StepLunge` only behind `if (_motor.IsDefender)`, so on an attacker
the key, the pad's left trigger and the touch layer's LUNGE button were all inert, **a dead
control on the thumb surface that exists because there is no keyboard.**

That is why this needs no `CLAUDE.md` § 4a work: no new `Verb`, so no pad answer, no thumb target,
no `InputAssetSync.Regenerate`. `NationalsHardeningTests
.TheSlideAddsNoNewVerbAndReusesADeadControl` asserts both halves.

### 146.2 ✅ EVERY NUMBER IS SOLVED FROM ONE THE GAME ALREADY HAD

`CLAUDE.md` § 4: *"Write the distance you want and solve for the speed; never hard-code a distance
beside a speed."*

| Constant | Value | Solved from |
|---|---|---|
| `SlideDistance` | **1.75 m** | `PickupRadius`. The slide converts the LAST APPROACH into a commitment, and the last approach is by definition where a pickup starts working |
| `SlideSpeed` | **10.247 m/s** | `sqrt(2 · Friction · SlideDistance)`. ⚠️ **Computed, not typed**, unlike `LungeSpeed` which is a literal 7.746 and would silently lie if `Friction` moved |
| `SlideActiveTime` | **0.342 s** | `SlideSpeed / Friction`: exactly how long the impulse takes to decay |
| `SlideRecoveryTime` | **0.608 s** | `(LungeChargeTime + LungeActiveTime) − SlideActiveTime` |
| `SlideCooldown` | **2.45 s** | `LungeChargeTime + LungeActiveTime + LungeCooldown` |
| `SlideSteerScale` | **0.35** | `LungeMinPower`, which is the game's existing answer to "how much of a committed move is still yours" |
| `SlideStaminaCost` | **25** | `ShoveStaminaCost`, the attacker's other committed verb |

**What it buys, measured:** walking 1.75 m at `Speed · AttackerSpeedScale` (2.53 m/s) is **0.69 s**;
sliding it is **0.34 s**. ⚠️ **So the advantage is about a third of a second, and it is bounded by
being less than one taya decision**: `PunchCooldown` is 0.9 s and `LungeChargeTime` is 0.5 s. The
attacker cannot outrun a read; they can beat a taya who committed late.

**What it costs:** the committed window is **exactly the taya's whole punish cycle** (0.95 s), so
a perfect read can always be cashed in, that is the arithmetic that stops this being a free
mobility buff. The cooldown carries the taya's own cooldown on top, so an attacker cannot slide
back out of the consequence the first slide invited.

### 146.3 ✅ THE PUNISHMENT IS COMMITMENT, NOT A STATUS EFFECT

`CharacterMotor.Commit(seconds)` narrows steering to `SlideSteerScale` and **leaves `CanAct()`
alone**. A committed attacker can still grab, still throw, and is still taggable: what they cannot
do is turn. A taya reading it knows where the body is coming out.

⚠️ **IT OVERLAPS BY `Max`, LIKE EVERY STUN** (`CLAUDE.md` § 4), and `ApplyStagger` releases it 
a staggered body is not a committed one, and leaving both running would be a punish that punishes
twice.

### 146.4 ✅ HOST-AUTHORITATIVE, AND THE PICKUP RULE IS NOT RESTATED

`Slipper.CanBeGrabbedBy` decides eligibility and `Slipper.HostGrab` performs it, exactly as a
walk-up does, so a slide cannot collect anything a walk-up could not. `MatchRpc
.RequestSlideServerRpc` carries a pose and a facing and **never names a tsinelas**, so a modified
client cannot ask for somebody else's.

⚠️⚠️ **IT RAYCASTS AND THE TAG SWEEP DOES NOT, WHICH IS NOT AN INCONSISTENCY.** Two bodies are
both pushed out of geometry by the physics engine, so a segment between them is a segment through
open street. A tsinelas comes to rest wherever it lands, including hard against the far side of a
wall, and a radius around a segment cannot see one. `RetrievalSlideTests
.ASlideCannotCollectThroughAWall` is the receipt.

### 146.4b ✅ AND AN AUDIT CAUGHT A DOUBLE AWARD ON THE RUN IT WAS WRITTEN

`SweepSlideRetrieval` fired its own `Hud.ReportStyle(..., "SIPA RESCUE!")` for one day.
`tools/audit_presentation_reach.py` reported it as **the only HOST-ONLY presentation call site in
the whole game** (98 sites, 97 reachable), and it was right twice over:

1. **The pickup already reports style.** `Slipper.HostGrab` calls `Carrier.NotifyHolding`, which
   fires `ReportStyle` on EVERY peer for every pickup however it happened. A second award here is
   one retrieval paid twice, which is § 57.3's fault arriving in the cosmetic bar.
2. **A call inside a `ShouldResolve()` gate is one player's.** `Hud.ReportStyle` does relay by
   default, so it would in fact have reached the seat's owner, but an audit that has to know
   that about every call site is an audit nobody can read.

**The callout still names the slide** and the award did not change: `NotifyHolding` picks
`"SIPA RESCUE!"` or `"SNATCH!"` off `CharacterMotor.IsCommitted`. One award, one funnel, and the
word says which retrieval it was. ⚠️ **Paying it more would be `docs/VISION.md` § 1.1's *"do not
give Classic powers"* arriving through the hype bar.**

Reading after: **97 sites, 97 reachable, 0 host-only.**

### 146.4c ⚠️ AND THE FIXTURE'S OWN FIRST RED WAS A TRAP WORTH WRITING DOWN

`RetrievalSlideTests` waited in FRAMES: `for (int i = 0; i < 60; i++) yield return null;`, on the
reasonable-sounding assumption that sixty frames is about a second. **In a `-batchmode` PlayMode
run there is nothing to present**, so the loop spins at thousands of frames a second and sixty of
them measured **0.03 s of game time**. The slide needs `SlideActiveTime`, 0.34 s, to decay its
impulse and cover its 1.75 m.

⚠️⚠️ **AND IT READ AS THE FEATURE BEING BROKEN FOR TWO RUNS**, because a bare `Assert.IsNotNull`
tells you nothing. The fix that found it was a failure message that names numbers, which is
`CLAUDE.md` § 2.3 applied to an assertion:

```
  the body travelled 0.197 m against a designed 1.75
  it ended 2.482 m from the tsinelas in 3D, against a CanBeGrabbedBy radius of 1.75
  slide cooldown left 2.42 (non-zero means the press WAS taken)
```

**0.197 m is one physics step at `SlideSpeed` exactly** (10.247 × 0.02), and the non-zero cooldown
said the press had been taken, so the two lines together ruled out every hypothesis about the verb
and left only the clock. ⚠️ **Two earlier guesses had already been spent on the wrong thing**: the
confinement box (only the DEFENDER is confined, so it never applied to an attacker) and the seat
lanes. **A number in the message would have skipped both.**

`Seconds()` and `SlideToFinish()` wait on `Time.deltaTime` now, and the second is derived from
`Balance.SlideActiveTime` so a retune cannot leave the fixture waiting for less than the move it
measures.

### 146.5 ✅ THE BOT IS TAUGHT IT, DELIBERATELY, AND ONLY WHERE A PERSON WOULD

`AIController.StepSlideIntent`, in `DoFetch`. An attacker bot could not have found this verb by
accident, `Verb.Lunge` is cleared for every plan that does not touch it and only `Hunt` (the
taya's) does, which is the safe default and is also why **nothing would ever have measured it**:
`BotBehaviourProbe` is the only thing in the repository that plays a whole match.

⚠️ **THE GATE IS THE SAME SENTENCE `DoFetch` ALREADY SPRINTS ON**: the shoe is inside a slide and
outside a walk-up, the run is contested or already late, the bot is facing it, and the bar is not
empty. A bot that slid whenever it could would arrive at every tsinelas with no stamina, which is
a worse retrieval than walking, and the probe would report the feature making the game worse.

### 146.6 ⚠️ OPEN: THE ART, AND THE NUMBERS AFTER A HUMAN HAS FELT THEM

- **It plays the lunge clip.** Both moves are a body-led dash and the rig has one; a slide of its
  own is art work rather than code work. A shared clip that reads correctly beats a wrong one.
- ⚠️⚠️ **THE FEEL IS `Attention.md`'S AND NOT THIS QUEUE'S.** Every number above is derived and
  every derivation is written down, and *"do NOT make the punishment so severe that nobody uses
  it, do NOT make it so safe that normal retrieval becomes obsolete"* is a judgement a probe
  cannot make. The multi-seed sweep (§ 16) is how a retune gets evidence; a person deciding it
  feels right is what the retune is for.

---

## 145 · THE HARDENING THAT COULD STILL PRODUCE FALSE CONFIDENCE ⚠️ IN PROGRESS, 2026-09-05, branch `main`

The 2026-09-04 pass (§ 143) built a great deal of gate. **Four pieces of it could still come out
green while proving less than they printed**, which is the same class of fault § 143 exists to fix,
one level in.

### 145.1 ✅ A QUALIFICATION MAY NOT CERTIFY A TREE IT CANNOT TIE TO A COMMIT

`tools/qualify.py` printed *"⚠️ Working tree was DIRTY at report time, N paths"* and then wrote
**QUALIFIED** underneath it. A note somebody has to read is not a gate, and every ⚠️ in this
repository exists because a note was not read once.

**The tree is a STAGE now**, first in the table, and there are three verdicts rather than two:

| Tree | Verdict |
|---|---|
| `clean` | the stages decide |
| `dirty` (tracked changes) | **NOT QUALIFIED**, whatever the stages say |
| cannot be established | **NON-QUALIFIABLE** when every stage passed |

⚠️⚠️ **`NON-QUALIFIABLE` IS NOT A FAILURE AND FOLDING IT EITHER WAY IS WRONG.** Calling it NOT
QUALIFIED says a test failed when none did; calling it QUALIFIED is the fault above. No `git` on
PATH, a source export and a failed `git status` are all states this can genuinely be in.

⚠️ **UNTRACKED FILES DO NOT FAIL IT.** `Logs/`, `Builds/` and a scratch file are not differences
in the source that was tested, and a gate that failed on those is a gate every developer learns to
pass with a flag.

⚠️ **NOTHING STOPS A LOCAL DIRTY BUILD.** `GameBuilder` records the tree state and builds anyway,
deliberately: building with uncommitted changes at a venue at 8 a.m. is a legitimate thing to do.
**The strictness belongs in the certification path.**

### 145.2 ✅ THE BUILD-SIDE DIRTY FLAG WAS A HEURISTIC THAT COULD NOT SEE AN ORDINARY EDIT

`GameBuilder.WorkingTreeIsDirty` compared `.git/index`'s write time against the branch ref's, to
avoid launching a process. **Two things were wrong with it and both failed towards "clean":**

1. ⚠️⚠️ **EDITING A TRACKED FILE DOES NOT REWRITE `.git/index`.** `git add` does. So the single
   commonest way a tree becomes dirty, open a `.cs`, change a number, build, left the index
   untouched and the stamp read `dirty: false`.
2. ⚠️⚠️ **A PACKED REF HAS NO LOOSE FILE** and the old body returned `false` outright when it
   could not find one, which is exactly the state a freshly cloned build machine is in.

**It asks `git status --porcelain` now**, with a ten-second bound, and answers **clean / dirty /
unknown**. `BuildIdentity.Record.treeState` carries the three-way answer and `dirty` becomes "not
proven clean", so every existing reader errs safe.

⚠️ **A PRE-2026-09-05 STAMP READS `unknown`, NOT `clean`.** Its `dirty: false` came from the
heuristic above, and believing it would carry that blind spot forward into the gate built to
replace it. `NationalsHardeningTests.AnUnknownWorkingTreeIsNeverReadAsClean`.

### 145.3 ✅ THE TOURNAMENT AUDIT COULD NOT SEE SWITCH NUMBER NINE

`audit_tournament_defaults.py` proved `TournamentPreset.Modifiers` and `TournamentGuard` agreed
with each other, in both directions. **A static added tomorrow to NEITHER file satisfies every one
of those checks**: it is not on the roster, so no case is missing, and it has no accessor, so no
accessor is dead. The whole failure mode that roster exists for lives exactly in that blind spot.

**So the audit DISCOVERS candidates now** and requires each to be declared or dismissed:

- Every **settable** `public`/`internal static bool` in `Assets/TumbangPreso/Runtime`.
- Every `-tp-` launch switch the runtime reads.
- `TournamentPreset.NotModifiers` is where one is dismissed, **with a written reason**, and
  `LaunchSwitchModifier` says which static a launch switch leaves set (or that it leaves none).

⚠️⚠️ **"SETTABLE" IS WHAT KEEPS THIS FROM BEING THE NOISY GATE THE BRIEF WARNS AGAINST.** There
are **41** static bools in the runtime and **13** are settable; the other 28 are derived
properties (`NetAuthority.IsHost`, `Panel.AnyOpen`, `PracticeSandbox.Active`) that nothing outside
can write, so nothing can LEAVE one set, which is the entire hazard.

**Current reading:** 8 modifiers, 20 exemptions with reasons, 8 read cases, 8 write cases, 19
launch switches discovered, **0 findings**. ⚠️ **Proven by adding one**: a throwaway
`public static bool SwitchNumberNine;` in the runtime made the audit exit 1 naming the file.

`NationalsHardeningTests.EverySettableStaticSwitchIsEitherAModifierOrWrittenDownAsNotOne` asserts
the same property from inside Unity.

### 145.4 ✅ THE REFEREE VERIFIER PRINTED MORE THAN IT GATED

`referee_run.py`'s table carried the protocol, the round, the seat roster, the characters and the
scores. **Its verdict asked about the referee's own slot, each client's slot and role, and the
per-seat character and taya flags between the two clients only.** So the REFEREE could disagree
with both clients about every seat in the game and the run came back green.

⚠️⚠️ **AND THE DISCRETE HASH WAS NEVER USABLE AS AN EQUALITY GATE, WHICH IS WHY IT WAS NOT ONE.**
`NetStateReport`'s own note says it is *"for the eye, not for an assertion"*, and the reason is
sharper than "peers cannot agree bit for bit": **discrete is not the same as constant.** The score,
the slipper states and the defender all move, and the referee outlives its clients by design, so
the three reports are written seconds apart. Comparing it would go red on a working link.

**So `NetStateReport` gained a STRUCTURAL hash** covering only what cannot change while a match
runs, the character in each seat, which seats are bots, and the protocol. The verifier sorts
every field into four kinds:

| Kind | Fields | How it is checked |
|---|---|---|
| **Constant** | protocol, mode, map, per-seat character, per-seat bot flag, structural hash | hard equality, all three peers |
| **Monotonic** | round, score | a peer that stopped LATER may not hold LESS |
| **Derived** | defender | `(round − 1) % 4` on each peer's OWN round, plus equality where the rounds agree |
| **Sampled** | discrete hash, slipper states, distances | printed, never gated, and a differing discrete hash prints as a NOTE saying why it is not a finding |

**New findings it can now make:** two clients reporting the same seat, any peer never reaching
round 1, a peer whose defender disagrees with its own round, a score that went backwards between
an earlier and a later sample, and the referee disagreeing with either client about any seat.

### 145.6 ✅ THE BOT SWEEP CAN ANSWER "DID THIS CHANGE MOVE ANYTHING", WHICH IT COULD NOT

`tools/bot_sweep.py` already ran the probe across fixed seeds and reported min, max, mean, median,
stdev and spread. **What it could not do was compare two of those**, which is the question anybody
actually has, and the join between the spread and the decision was a person doing arithmetic in
their head, which is exactly where *"58 to 100 throws"* gets read as *"the change made it worse"*.

`--compare BEFORE.json AFTER.json` reports, per metric:

- **Welch's t**, not a percentage. ⚠️ **A 20 per cent move on a metric whose own spread is 20 per
  cent is nothing; the same move on one that never varies is everything**, and a percentage cannot
  tell those apart. Welch's form is the one that does not assume the two arms share a variance,
  which is the whole point of comparing a retuned game against a shipped one.
- **Three verdicts, not a boolean.** `MOVED`, `no change measured`, and **`UNDER-SAMPLED`**, which
  is what two runs an arm earns: § 16's own answer is three for anything worth 20 per cent, and a
  t-test on n = 2 is arithmetic performed on nothing.
- ⚠️ **No SciPy and no table.** |t| >= 2.0 is roughly the two-sided 5 per cent point at these
  degrees of freedom, and **the statistic is printed beside the verdict** so a reader who wants a
  real threshold has the number to apply it to. Importing a stats package into a repository whose
  gate is `python` on a venue laptop is a trade this file will not make.

⚠️⚠️ **AND A SWEEP CARRIES A CONFIG DIGEST NOW, NOT ONLY A SHA.** A docs commit moves the SHA and
changes nothing about the game, so two sweeps at two SHAs could be measuring one game or two and
nothing said which. The digest is `Balance.cs`, `AiTuning.cs`, `MatchRules.cs` and `ThrowRules.cs`
hashed as TEXT: two sweeps with the same digest were measuring the same game, and the comparison
says so out loud (*"any difference below is the noise floor being measured twice"*).

⚠️ **`MOVED` IS NOT `WORSE` AND THE TOOL SAYS SO.** Whether more throws and fewer tags is the game
anybody wants is a design judgement; `docs/VISION.md` § 2 is where that argument is had.

⚠️ **AND `no change measured` IS NOT `NO CHANGE`.** With five seeds an arm it cannot see a move
smaller than roughly the spread. Buy more seeds before concluding a change did nothing.

**Verified on synthetic sweeps before spending Unity launches on it**: two arms drawn from the same
distribution read `no change measured` on every metric; an arm with a real +66 per cent on throws
read `MOVED` at |t| 10.4. ⚠️ **A real sweep is 5 to 8 Unity launches a side and was not run in this
pass**; the harness is the deliverable and § 16 is the standing argument for using it.

### 145.4b ✅ MEASURED AND FIXED: THE ROSTERS AGREED; `IsBot` WAS BEING GATED AS IF IT WERE CONSTANT

**First run after the rewrite, `87346b8`, one referee and two clients on loopback for 45 s:**

```
referee   : slot -1  proto 24  round 1  active True  defender 0  struct FC4251A5
client1   : slot  1  proto 24  round 1  active True  defender 0  struct B05920F1
client2   : slot  2  proto 24  round 1  active True  defender 0  struct AFC81248

referee   : seats 0[c0 bot TAYA]  1[c3 bot]  2[c0 bot]  3[c3 bot]
client1   : seats 0[c0 bot TAYA]  1[c3 hum]  2[c0 hum]  3[c3 bot]
client2   : seats 0[c0 bot TAYA]  1[c3 bot]  2[c0 hum]  3[c3 bot]
```

**Everything the old verdict checked passed**: the referee is seatless, both clients hold distinct
seats, all three reached round 1, the protocol, the mode, the map, the characters and the derived
taya all agree, and no score went backwards. **The three new findings are all the same fact:**

| Seat | referee | client1 | client2 |
|---|---|---|---|
| 1 | bot | **human** | bot |
| 2 | bot | **human** | **human** |

⚠️⚠️ **EACH PEER CALLS ITS OWN SEAT HUMAN AND DISAGREES ABOUT EVERYBODY ELSE'S.** Client1 knows
both peers; client2 knows only itself; the referee, which holds no seat, calls all four bots. That
is `IsBot` being DERIVED per peer from that peer's own view of the lobby roster rather than agreed.

⚠️ **AND IT IS NOT COSMETIC.** `IsBot` is on the wire and in `IntegrityRules.Digest`, and
`BotFillRules.Weight` scales a rating by how many seats were people. Three peers holding three
human counts for one match submit three different weights for it, and § 144.7's whole handover
ruling rests on the same flag.

⚠️⚠️ **DO NOT FIX THIS FROM THIS ENTRY. THE FIRST STEP IS ONE MORE RUN.** `-tp-allbots` puts an AI
on every seat on every peer, including each client's own, so part of what is printed above is the
HARNESS rather than the game, and guessing which part is how § 144.8 got handed on with a warning
against retuning before measuring. **Run the same three processes without `-tp-allbots`, with the
clients idle**, and compare: if the rosters still disagree it is the game, and if they agree it is
the switch and the verifier needs to know that.

**Done looks like:** three peers agreeing on which seats are people, or a written reason why a
seatless referee legitimately cannot know, with the verifier taught that reason rather than having
the check removed.

#### The second run, 2026-09-05, `837eb0a` + this session's harness change

`python tools/referee_run.py --seconds 45 --no-allbots`, one referee and two clients on loopback,
the clients holding their own seats and standing still:

```
referee   : slot -1  proto 24  round 1  active True  defender 0  sampled 60.0 s  struct FC4251A5
client1   : slot  1  proto 24  round 1  active True  defender 0  sampled 45.2 s  struct 2408EEE1
client2   : slot  0  proto 24  round 1  active True  defender 0  sampled 45.0 s  struct 2408EEE1

referee   : seats 0[c0 bot TAYA 330]  1[c3 bot atk 100]  2[c0 bot atk 200]  3[c3 bot atk 0]
client1   : seats 0[c0 hum TAYA  30]  1[c3 hum atk   0]  2[c0 bot atk 100]  3[c3 bot atk 0]
client2   : seats 0[c0 hum TAYA  30]  1[c3 hum atk   0]  2[c0 bot atk 100]  3[c3 bot atk 0]
```

⚠️⚠️ **THE TWO CLIENTS AGREE EXACTLY, DOWN TO THE STRUCTURAL HASH.** The `-tp-allbots` run's
three-way disagreement was the SWITCH: it makes `MatchInstaller.HumanSeat` answer -1, so a peer
running it calls its own seat a bot as well as everybody else's, and half of what that table
printed was the harness. § 145.4b was right to refuse to fix anything from it.

⚠️⚠️ **AND THE REFEREE'S DISAGREEMENT IS A CLOCK, NOT A ROSTER.** Its own log:

```
1790: arena installed: LocalSlot=-1 host=True allBots=False | s0..s3 all bot
1828: [Net] 2 connected, seat 0
8741: [Net] 2 connected, seat 1
17386: [Slice] round 1 begins, taya is seat 0
17397: [Handover] seat 0 was rated 1500; a Normal bot is finishing it.
19127: [Handover] seat 1 was rated 1500; a Normal bot is finishing it.
```

The referee outlives its clients **by design** (`referee_run.run` gives it their head start back
plus a margin, because a referee that quits first tears the transport down under them). Both
clients wrote at 45 s and quit; the referee sampled at 60 s, by which time both chairs had been
correctly handed to bots. **Every one of the three findings was the verifier gating a field that
is not constant.**

⚠️ **`NetStateReport.StructuralHash`'s OWN NOTE ALREADY SAID SO AND IT HASHED THE FLAG ANYWAY**:
*"`MatchRpc.HostPeerLeft` can flip a bot flag mid-match when a chair is handed over"*, in a method
whose summary is "the part of the state that cannot change while a match is running".

#### And one real defect came out of it

⚠️⚠️ **A HOST THAT OPENS THE ARENA BEFORE ITS PEERS ARRIVE RECORDED EVERY PLAYER'S CHAIR AS A
BOT'S, FOR THE WHOLE MATCH.** Line 1790 above is the referee installing four seats before line
1828's first connection: `MatchInstaller.BuildSeat` asks the lobby who is sitting where, the answer
at boot is "nobody", and `CharacterMotor.NoteSeatOrigin(Human)` is a deliberate no-op, so
`SeatOrigin` stayed `Bot` even after `HostTakeSeatBackFromBot` cleared `IsBot`. That is **every
`-tp-dedicated` referee and every `-tp-autostart` host**, and `SeatHandover.RatingMovesFor` and
`SeatHandover.HumanSeats` both read that field: a referee-hosted bracket match would have submitted
four bot seats and moved nobody's rating.

#### What shipped

| | |
|---|---|
| `Core.SeatHandover.APersonSatHere` | the one thing about a seat's driver that IS constant for a match. `Bot` means nobody ever sat there; `Human` and `HandedToBot` both mean somebody did, and a chair somebody sat in never becomes one nobody sat in |
| `CharacterMotor.NoteSeatClaimedByAPerson(midMatch)` | corrects an install-time guess. Before the whistle a chair changing hands is the roster settling, so it is `Human`; after it a bot has already played part of the match in that chair, so it is `HandedToBot`. ⚠️ **A handover is never walked back** |
| `MatchRpc.HostTakeSeatBackFromBot` | calls it, so the host's own copy is corrected on the join it already handles |
| `MatchRpc.ApplyRosterToLiveSeats` | calls it, so a client that joined before the fourth player corrects its copy from the same broadcast roster. **One authoritative source, applied on every peer** |
| `NetStateReport` | prints an `origin` column beside `bot`, and the STRUCTURAL hash folds in `APersonSatHere` instead of the live flag |
| `referee_run.py`, `net_matrix.py` | gate the persistent fact and report the live flag as drift, with the origin printed beside it |

⚠️ **THE VERIFIER WAS NOT WEAKENED.** It gates a strictly stronger property than before: two peers
that genuinely disagree about whether a person ever sat in a chair are still a finding at any
instant, and the old check could not survive a legitimate departure at all.

#### THE VERIFICATION, `94c8fb54`, THREE PROCESSES, NO `-tp-allbots`

```
referee   : slot -1  proto 24  round 1  active True  defender 0  sampled 60.0 s  struct 8FC5A151
client1   : slot  0  proto 24  round 1  active True  defender 0  sampled 45.1 s  struct 8FC5A151
client2   : slot  1  proto 24  round 1  active True  defender 0  sampled 45.0 s  struct 8FC5A151

referee   : seats 0[c0 bot TAYA 70]  1[c3 bot atk   0]  2[c0 bot atk 100]  3[c3 bot atk 100]
client1   : seats 0[c0 hum TAYA 30]  1[c3 hum atk   0]  2[c0 bot atk   0]  3[c3 bot atk 100]
client2   : seats 0[c0 bot TAYA 30]  1[c3 hum atk   0]  2[c0 bot atk   0]  3[c3 bot atk 100]

referee_run: 0 finding(s)
```

⚠️⚠️ **ALL THREE STRUCTURAL HASHES ARE `8FC5A151`.** That is the acceptance criterion this entry
was opened with: *"three peers agreeing on which seats are people"*. The referee, which holds no
seat and outlives both clients, now agrees with both of them about the persistent human/bot meaning
of every chair.

⚠️ **AND THE LIVE FLAGS STILL DIFFER, WITH THE REASON PRINTED BESIDE THEM**, which is the finding
this entry actually produced:

```
note : seat 0 is driven by different things on different peers (referee bot, client1 hum,
       client2 bot), with origins referee HandedToBot, client1 Human, client2 Human. That is
       a handover or a roster arriving late, not a disagreement: only the origins above are gated.
```

The referee reads `HandedToBot` because by 60 s both players had quit and their chairs had
correctly been handed over; the clients read `Human` because at 45 s they were still in them.
**Both are true and they are the same fact at two instants.**

#### AND THE FIX HAD A SECOND HALF THE FIRST RUN FOUND

The run at `ec44867e` had the referee and ONE client agreeing and the other not, and that client's
own report read `1  3 False  Bot`: a seat it was driving, recorded as a bot's.
`MatchInstaller.BuildSeat` writes `SeatOrigin` from the roster it holds when the arena opens, and
on a client that is **before the seat assignment arrives**, so `HumanSeat` is still its default 0
and only the client that happened to be given seat 0 came out right.
`ApplyRosterToLiveSeats` cannot correct it either, because it skips `NetAuthority.LocalSlot` on
purpose. **`ApplyRebindLocalSeat` owns that chair and records the claim now**, which is the third
of the three call sites and the one that could only be found by running it.

### 145.7 ✅ `net_matrix.py` PRINTED `DIVERGED` AND EXITED 0

Every scenario carried an `expect` string that said what the run was supposed to show. **Nothing
read it.** `describe()` returned a sentence, `emit()` printed it, and `main()` returned 0 whatever
was in it, so a shell running the disconnect matrix was told the whole table passed while the
table said otherwise.

Three things are separate now and were one:

| | |
|---|---|
| `describe()` | what happened, for the column a person reads |
| `evaluate()` | whether that is what the row predicted. Returns `(ok, faults)` |
| `main()` | the process verdict, which is the OR of every row |

⚠️⚠️ **A DELIBERATE KILL IS NOT A FAILURE, WHICH IS WHY THE EXPECTATION IS PER PEER RATHER THAN ONE
BOOLEAN.** *"The client wrote no report"* is the CORRECT outcome of the row that kills the client
and a hang on every other row. *"The client fell back to hosting"* is the defined correct end of
the host-loss row and the **worst possible result** of the 600 ms row. So each scenario names
`host` (`referees` / `gone`) and `client` (`joined` / `gone` / `terminal`), and the pair is checked.

⚠️ **"STILL ALIVE" IS NOT THE CLAIM AND `describe` ALREADY SAID SO IN CAPITALS.** A host frozen on
a dead round looks identical in a process list, so `HOST_REFEREES` requires sampling **past** the
disturbance, a live round, and at least one seat that travelled a metre.

⚠️ **`--summarise` GATES TOO.** It re-reads reports already on disk, so a constant return there was
the quietest possible way to get a green shell out of a red measurement.

⚠️ **THE OUTPUT DID NOT GET SMALLER.** The verdict is a new column and every failing row's faults
are printed in full underneath the table.

### 145.8 ✅ THE COLD START PROVED HERO STRIKE ON macOS, NOT CLASSIC ON WINDOWS

The recorded green run at `87346b8` is real and is good coverage: a shipped player launched,
reached round 1, moved four seats and scored. It is also **a macOS player playing HERO STRIKE**,
and `docs/VISION.md` § 1.1 says in as many words that **Classic is the tournament ruleset**. So it
says nothing about the path a bracket match takes.

**What shipped:**

- `-tp-tournament` (`NetBootstrap.TournamentSwitch`) applies `TournamentGuard.Apply()` before the
  map is chosen, which pins the tournament rule set and clears every practice or debug modifier.
  ⚠️ It is on `TournamentPreset.NotModifiers` with its reason: it is the one launch switch that
  can only make a machine **more** tournament-legal.
- `NetStateReport` prints `tournament ruleset` (`OK` or the refusal sentence, flattened to one
  line), `tournament modifiers` (the names not at their safe value, or `none`) and
  `build identity`. **A harness cannot assert what it cannot read.**
- `tools/cold_start.py --tournament` drives that, and `--nationals` adds the artifact gates: the
  release build target, a `clean` stamped tree, the protocol this source reads, and a UGS project
  that matches `ProjectSettings.asset`.

⚠️⚠️ **`--tournament` DROPS `-tp-allbots` ON PURPOSE AND THAT IS THE POINT OF IT.**
`GameLaunch.AllBots` is one of the switches the preset forbids, so a "tournament" cold start that
set it would be asserting the preset while breaking it. Without it seat 0 is a person standing
still and the other three are bots that drive: a live round with every modifier at its safe value
at once. `TOURNAMENT_ALLOWED_MODIFIERS` is the **empty set**, because a gate that forgave its own
modifier would be § 145.3's hole one level up.

⚠️ **THE SMOKE PATH IS KEPT AND IS NOT REDUNDANT.** It is the only row that exercises four DRIVEN
seats, and `-tp-allbots` is what gets a fourth body moving.

#### THE RUN, `94c8fb54`, WINDOWS, `docs/reports/cold-start-94c8fb54c369.md`

```
## Verdict: PASS

| the artifact is a nationals candidate        | PASS | SHA 94c8fb54c369, tree clean,
|                                              |      | protocol 24, StandaloneWindows64,
|                                              |      | UGS dcf0831e-…/production
| launches and identifies itself               | PASS | clean
| reaches the arena and exits cleanly          | PASS | clean
| a real CLASSIC tournament round became active| PASS | round 1, active, 3 seats driving

mode                 : Classic
tournament ruleset   : OK
tournament modifiers : none
build identity       : TUMBANG PRESO 1.0.0 | 94c8fb54c369 | protocol 24 | StandaloneWindows64

seat   char   bot      origin  taya   score  travelled
0         0 False       Human  True      30        0.5     <- the person, standing still
1         3  True         Bot False     100       37.2
2         6  True         Bot False       0       31.3
3         9  True         Bot False       0       36.2
```

⚠️⚠️ **AND IT FAILED FIRST, FOR A REASON OF ITS OWN INVENTION, WHICH IS WORTH RECORDING BECAUSE IT
IS THIS SESSION'S OWN FAULT CLASS ARRIVING FROM THE OTHER SIDE.** The first run reported
*"only 0 seat(s) travelled more than a metre, so the bots were not driving"* against the capture
above, which says three of them travelled thirty-plus metres. `NetStateReport` gained an `origin`
column that day and `cold_start.read_state`'s seat regex had not been told; it matched nothing, so
the count was zero. `tools/referee_run.py` had the identical fault in the identical hour.
**A gate that fails for a reason it made up is the same defect as one that passes for one**, and
`tools/audit_harness_contracts.py` now drives both parsers over a real report shape and over a
pre-column one, 152 checks.

### 145.9 ✅ AN UNTRACKED SOURCE FILE WAS NOT A DIRTY TREE

§ 145.1 made the tree a stage and then wrote, as a reason: *"⚠️ UNTRACKED FILES DO NOT FAIL IT.
`Logs/`, `Builds/` and a scratch file are not differences in the source that was tested."* True of
those three and **false of the general case, in a Unity project especially**:

- an untracked `.cs` anywhere under `Assets/` or `Packages/` **compiles**;
- an untracked `.shader`, `.prefab`, `.unity`, `.mat` or anything under `Resources/` or
  `StreamingAssets/` **ships inside the player**;
- `ProjectSettings/` decides the splash, the app version, the build target and **the UGS project a
  join code is resolved in**.

Every one of those changes the artifact while HEAD points at a commit that does not contain it, and
the report printed `SHA X / tree clean` over the top.

⚠️⚠️ **THE RULE IS DEFAULT-DENY AND THAT IS THE WHOLE DESIGN.** The obvious shape is a list of
directories that ARE source, and it is the brittle one: somebody adds `Assets/NewThing/` next year,
nobody edits the list, and the gate goes quiet in the one direction nobody checks. Under
default-deny a path nobody has thought about is DIRTY, which is loud, and the only way to make the
gate quieter is to add a root **with a written reason**.

⚠️ **`.gitignore` STILL DOES THE BULK OF IT.** `--porcelain` does not list ignored files at all, so
`Library/`, `Temp/`, `Logs/`, `Builds/`, both build stamps and the shader-variant collection never
reach the classifier.

⚠️ **`docs/reports/` IS THE ONE NON-OBVIOUS EXEMPTION AND IT HAS TO BE THERE.** `qualify.py` writes
`docs/reports/qualification-<sha>.md` as its own last act, so without that row the first run leaves
the tree non-certifiable and every run afterwards fails on the evidence the previous one produced.

**Both sides share one rule.** `Core.WorkingTreeRules` is the C# copy (used by
`GameBuilder.WorkingTreeState`), `qualify.py` holds the Python one, and
`tools/audit_harness_contracts.py` plus `NationalsHardeningTests
.ThePythonQualificationSharesTheOneWorkingTreeRule` assert the two lists are identical.
⚠️ `IntegrityRules.Digest` is the precedent: a rule written twice with nothing comparing the two is
a rule that silently disagrees with itself.

### 145.10 ✅ A DIRTY ARTIFACT COULD QUALIFY, AND UGS IDENTITY WAS NOT PART OF CROSSPLAY

**The scenario the gate could not see:** HEAD is X, edit a tracked gameplay `.cs`, build, revert the
edit, run the qualification. The tree is clean, the artifact's stamp says X, every SHA comparison
in the gate passes, and the player on disk contains code that is in no commit. `treeState` is the
only field that can refuse it and nothing read it.

`qualify.candidate_faults` refuses an artifact for any of: no stamp at all, a missing or blank
required field (`sha`, `protocol`, `target`, `appVersion`, `ugsProject`, `ugsEnvironment`,
`builtAt`, `treeState`), `treeState` dirty, `treeState` unknown or absent, `treeState clean` beside
`dirty true` (which means something other than `GameBuilder` wrote the record), a SHA that is not
the candidate's, or a protocol the source has moved past. `--nationals` adds the release target.

⚠️⚠️ **AND CROSSPLAY IS FIVE FIELDS RATHER THAN TWO.** Two artifacts on one commit and one protocol
can still be two games: peers that resolve Relay and Lobby against **different UGS identities do
not refuse each other, they never find each other's rooms**, and `CLAUDE.md` § 4a records that this
reads as an EMPTY LOBBY rather than as an error. `CROSSPLAY_FIELDS` is `sha`, `protocol`,
`ugsProject`, `ugsEnvironment`, `appVersion`, and for a candidate a blank on either side is a
finding of its own, because **a blank looks like agreement**.

### 145.11 ⚠️⚠️ P0 CLOSED: THE RETRIEVAL SLIDE WAS A FREE DASH OVER THE WIRE

`StepSlide` refused a press with no retrievable tsinelas ahead. `HostResolveSlide` checked the
role, the cooldown, the hand, the fatigue and the bar, and **never asked whether there was anything
to retrieve at all**. So the local path could not dash for free and a modified client asking over
the wire could: the host applied `SlideSpeed` down the requested facing on request, which is 1.75 m
of host-authoritative mobility with nothing to collect. `docs/VISION.md` § 1.1 forbids Classic a
power, and a networked-only free dash is a power only a cheat has.

**One rule now, `CombatVerbs.SlideMayStartFrom(from, facing, out target)`**, called by the press and
by the host. It asks: cooldown clear, attacker, `CanAct()`, not fatigued, hand empty, and a legally
retrievable tsinelas along the projected slide **that is not behind world geometry**.

⚠️⚠️ **AND THE PREDICATE NOW RAYCASTS, WHICH IT DID NOT.** The sweep checked line of sight and the
press predicate did not, so a player on the wrong side of a jeepney predicted a slide, spent 25
stamina, narrowed their own steering to 0.35 for most of a second and collected nothing. That is
not a fairness bug, it is the verb feeling broken: the local path promised what the host was always
going to refuse.

⚠️ **THE PICKUP RULE IS STILL NOT RESTATED.** `Slipper.IsGrabbableIgnoringReach` is
`CanBeGrabbedBy` with the distance clause taken out, and `CanBeGrabbedBy` is now defined in terms
of it, so there is one owner of "whose shoe is this" and the slide widens only the REACH, from a
radius around the body to the same radius around the ground it covers. `AnySlideTargetAhead` used to
test `State == Loose` by hand, which is one clause of a four-clause rule that happened to agree.

### 145.12 ⚠️⚠️ P0 CLOSED: THE SLIDE'S REFUSAL COULD NEVER BE ROLLED BACK

`MatchRpc.OnVerbDeniedMsg` bounded the incoming verb byte at `DeniedVerb.Shove`, which is **2**,
and `Slide` is **3**. Every slide refusal the host sent was discarded by the client one function
before `RollBackRefusedVerb`, so the whole `case DeniedVerb.Slide` arm, which returns a 2.45 s
cooldown, 25 stamina and a commitment that narrows steering for most of a second, **was unreachable
code from the day it was written**.

⚠️ **AND `_denialsTaken` WAS `new int[3]` ON THE LINE UNDER THE COMMENT WARNING ABOUT A LITERAL 3.**
`CountDenial` returns early past the end of the array, so a slide refusal a client DID take back
was counted nowhere, and the absence read as "the slide is never refused". Both are the enum's own
length now.

### 145.13 ✅ `cold_start --clean-profile` COULD DELETE THE ONLY BACKUP

The shadowed-variable fault that destroyed a real Mac profile was already repaired. **The path it
left behind was still unsafe:** the backup directory was a CONSTANT, and the restore ran
`rmtree` on whatever was already sitting on it. A pre-existing `<profile>.coldstart-backup` is not
junk. It is what a previous crash, an interrupted restore, a power cut or a half-finished manual
recovery leaves behind, and in every one of those cases **it is the only copy of somebody's
settings, rebinds and career there is.**

`ProfileVault` has two properties and neither of them is "be careful":

1. **A backup directory is unique per run**, timestamp plus a GUID, so nothing it creates can
   collide with anything anybody left behind and a stale backup is never touched again. It writes
   `.coldstart-restore.json` inside itself naming the path it came from.
2. **`rmtree` is never pointed at the live profile or at a backup.** The live profile is MOVED to a
   discard path and deleted only after the restore has already succeeded, so a failure at any point
   leaves two recoverable copies on disk and an error naming both.

`tools/audit_harness_contracts.py` drives it over temporary directories: no profile, the ordinary
round trip, a pre-existing backup, an exception in the run body, three shapes of malformed handle,
and a restore that fails half way. It also asserts there is exactly one `rmtree` CALL in the file
and that it targets a discard path.

### 145.14 ✅ THE AUDIT OF THE GRADERS

`tools/audit_harness_contracts.py` is new and gating. The other `audit_*.py` files read the GAME's
source; this one reads the harnesses that grade it, because three of them were caught doing exactly
what the game's own gates were caught doing. **138 checks.**

⚠️ **NONE OF IT COULD BE ASSERTED BY RUNNING THE HARNESS.** A net matrix row is fifteen minutes of
two game processes and a cold start is a minute of a real player and a real profile directory. Both
are graded by pure functions over a parsed report or a path, so the verdicts are driven over
synthetic inputs in milliseconds. `tools/bot_sweep.py`'s comparison was verified the same way before
a single Unity launch was spent on it.

---

### 145.5 ⚠️ OPEN: `qualify.py` STILL HAS TO RUN ON A WINDOWS MACHINE FOR A NATIONALS CANDIDATE

Its Unity and dotnet paths resolve per platform now, and `BUILD_TARGET` follows the machine
(`OSXUniversal` here, `Win64` there) and is **named in the report** rather than assumed. ⚠️ **The
report says out loud when the target is not the nationals one.**

**What is still true and is a machine fact rather than a defect:** `CLAUDE.md` § 7's Mac has no
Windows Standalone module and no dotnet, so a certification run for the shipped Windows player has
to happen on a Windows machine. `--stage core` reports that honestly now instead of failing.

---

## 144 · THE TWO ACCOUNT-GATED DOWNLOADS LANDED, AND THE AUDIO GATE WAS GRADING A COPY THE GAME CANNOT LOAD ⚠️ IN PROGRESS, 2026-09-04, branch `main`

🧑 signed in to Freesound in the session's browser and asked for the sixteen recordings to be
fetched from there, and uploaded the jeepney himself. `Attention.md` § 11 had both as
person-only work; both are done and that section now records the RULE rather than the task.

### 144.1 ✅ THE SIXTEEN RECORDINGS, AND 42 CUES THAT WERE SYNTHESISED ARE SOURCED

All sixteen `Asset_Sourcing.md` § 5.2 files are in `scratchpad/asset-src/freesound/`, verified
against the format and duration stated in their own rows to three decimal places. **42 cues
re-sourced**: all 18 `sfx_cast_*`, all 12 `sfx_var_*`, and `sfx_fire_whoosh`, `sfx_ice_form`,
`sfx_ice_freeze`, `sfx_ice_shatter`, `sfx_barricade_raise`, `sfx_thunder_impact`,
`sfx_lightning_strike`, `sfx_hex_cast`, `sfx_hex_afflict`, `sfx_blink_arrive`, `sfx_quake_slam`
and `lata_seal`.

⚠️⚠️ **THIRTEEN OF THE SIXTEEN ARE USED AND THE THREE THAT ARE NOT EACH CARRY A REASON.** The
tin can is the one worth knowing: § 5.2 names it for `lata_impact` and `lata_knockdown`, and
§ 5.4 records those exact cues being **rejected by ear** on 2026-09-03 and restored. **A source
table written before a listening test does not overrule the listening test.** The basketball and
the crowd cheer have no cue to go to: there is no plaza-ambience cue and no crowd-bed cue in
`Resources/Sfx`, and wiring them would mean inventing a cue nobody asked for.

⚠️ **`Slice(src, rank=n)` IS HOW SIX HEROES AVOID SOUNDING LIKE ONE.** § 5.3 says *"Do not reuse
one full cue for all three powers"* and *"Do not give all three abilities the same witch sound"*,
against thirteen usable recordings for thirty cues. A slice takes the **rank-th loudest
non-overlapping window** of a recording, so "the third loudest 1.1 s of the earthquake take" is
reproducible, is different material from the first, and cannot land on silence the way a typed
start time can. ⚠️ **Non-overlapping is the half that matters**: ranking every sample offset
returns rank 0, 1 and 2 all sitting on the same transient a millisecond apart.

⚠️⚠️ **AND EVERY ONE OF THE 42 IS PROVISIONAL.** `CLAUDE.md` § 6: sourced SFX are provisional
until 🧑 hears them in play. `Asset_Sourcing.md` § 5.5 lists them.

### 144.2 ✅ THE GENERATOR DECAYED A CUE BY ITS OWN GAIN ON EVERY RE-RUN

⚠️⚠️ **FOUND BY RUNNING IT TWICE, WHICH NOBODY HAD DONE.** `peak_of` read the peak of the file it
was about to OVERWRITE and the row's `gain` was multiplied onto it, so a second run multiplied
the gain again. `tag` at gain 0.9 goes **0.850, 0.765, 0.688, 0.620**, and the sixth run is half
the level `AudioCues.TrimDb`'s clipping measurement was taken at. Nothing warns, every row prints
a plausible number, and the only symptom is that the game gets quieter in patches.

✅ `tools/assets/cue_reference_peaks.json` holds the FINAL target per cue, written once, seeded
from the shipped mix for the 21 rows that already carried their gain. A second run is now a
byte-for-byte no-op, and that is asserted by running it twice and diffing.

⚠️ **THE FIRST ATTEMPT AT THIS LEDGER STORED THE PRE-GAIN REFERENCE AND DECAYED EXACTLY AS
BEFORE.** What has to be stable across runs is the number the file ENDS UP at, so that is the
number written down.

### 144.3 ⚠️⚠️ OPEN: BOTH AUDIO GATES WERE READING A DIRECTORY THE GAME CANNOT LOAD FROM

**This is the big one and it is `docs/TODO.md` § 114's fault on a whole subsystem.**

`AudioDirector` reaches every cue through `Resources.Load<AudioClip>($"Sfx/{stem}")`, and
`Resources.Load` can only resolve inside a folder literally named `Resources`. The cues the game
plays are `Assets/TumbangPreso/Resources/Sfx/`. **Both gates pointed at
`Assets/TumbangPreso/Art/audio/sfx/`, which is not under a `Resources` folder and is therefore
unreachable at runtime:**

- `tools/audit_cue_audio.py`, whose whole job is *"Does each cue file actually contain an
  audible, unclipped sound?"*
- `AudioCueCheck`, which is one of the eight editor checks in `Checks.RunAll` and gates a build.

⚠️⚠️ **AND THE TWO COPIES HAD ALREADY DRIFTED BEFORE THIS PASS: 21 of 117 cues differed**, which
is the 2026-09-03 source pass writing to `Resources/Sfx` while both gates went on grading the
untouched originals. Every DC-offset flag the audit was reporting was against a file the player
has never heard.

✅ **Both moved to `Resources/Sfx`.** The audit's flag count went **11 to 6**, and the six are all
pre-existing and none of them is from this pass.

⚠️⚠️ **OPEN, AND IT NEEDS HIS EAR RATHER THAN A COMMIT: THREE OF THE SIX ARE THE PROTECTED
`ui_*` FILES.** `ui_click` sits at a DC offset of **-0.121**, `ui_back` at -0.109 and `ui_hover`
at -0.101, which is a thump on every press of the three most-heard sounds in the game. They are
the originals he asked for back by name (§ 5.4, `CLAUDE.md` § 6), so **removing the offset means
rewriting his preferred files.** It is a one-line change to subtract the mean and it does not
alter the character of the sound at all; it removes a click. **Ask before doing it.** The other
three are `boot_sting`, `match_win` and `round_win`, which no generator owns.

### 144.3b ✅ MEASURED 2026-09-05: IT IS A MIRROR, THREE GENERATORS WERE STILL FEEDING IT, AND IT HAS DRIFTED

**The row asked for a decision and refused to accept an inference. Here is the measurement.**

| | |
|---|---|
| Files each side | **117 and 117**, none missing from either |
| Byte-identical | **56** |
| **Differing content** | **61** |
| Written by | `generate_hero_audio.py`, `generate_ability_audio.py`, `generate_skill_audio.py`, all three through an `OUT_DIRS` list naming BOTH folders |
| Written by (the sourced pass) | `build_ability_audio.py`, whose `SFX_DIR` is **`Resources/Sfx` alone** |
| Read by the game | **nothing.** `Resources.Load` can only reach a folder called `Resources` |

⚠️⚠️ **SO THE ROW'S OWN PREMISE WAS HALF WRONG: IT IS NOT TRUE THAT "NOTHING WRITES" IT.** Three
generators did, and each carried the same comment: *"The two places the game reads sfx from.
`AudioCueCheck` validates cues against files in both directions"*. **Both halves of that sentence
had stopped being true**: the game never read it, and `AudioCueCheck` and `audit_cue_audio.py` were
moved to `Resources/Sfx` on 2026-09-04 precisely because grading that folder was grading files no
player can hear.

⚠️⚠️ **AND THE 61 DIFFERING FILES SAY WHAT IT ACTUALLY IS.** The 2026-09-03 sourced pass writes only
`Resources/Sfx`, so `Art/audio/sfx` holds the **synthesised originals** of the cues that pass
replaced. It is neither an authored master (nothing authors into it that does not also write
`Resources/Sfx`) nor a copy (it disagrees on more than half its contents). It is a mirror that was
still being fed by the older generators and abandoned by the newer one.

#### THE DECISION

1. ✅ **It is no longer fed.** All three `OUT_DIRS` lists name `Resources/Sfx` alone, and the false
   comment is replaced with the measurement in `generate_hero_audio.py`.
2. ⚠️ **It is NOT deleted, and that is 🧑's call rather than caution.** `CLAUDE.md` § 6:
   **sourced SFX are provisional until he hears them in play**, twenty-four are still awaiting
   exactly that judgement (`Attention.md` § 13), and this folder is the convenient A/B against
   them: it is the pre-replacement sound for 61 cues, on disk, one file open away. The canonical
   restore point is git at `ee8bced^`, which `CLAUDE.md` § 6 already names, so nothing is lost by
   deleting it either. **What was actually costing something was feeding it**, because a folder
   being written to looks authoritative, and that is fixed.

**Done looks like:** 🧑 says the sourced cues are keepers, and then the folder goes in one commit.
Until then it is inert rather than misleading.

---

⚠️ **AND `Art/audio/sfx` IS NOW A 117-FILE DUPLICATE THAT NOTHING LOADS AND NOTHING WRITES**
except `tools/generate_hero_audio.py`. It is left in place rather than deleted, because deleting
117 audio files on an inference is exactly the kind of move this repository writes ⚠️ notes
about. **Decide what it is for, or delete it, but do not leave two copies with two different
answers.**

### 144.4 ✅ THE JEEPNEY SHIPS AS DELIVERED, AND § 6.0 EXISTS BECAUSE THE FIRST ATTEMPT DID NOT

Maclin Macalindong's CC BY jeepney replaces the distant north `van` on Ilalim ng Tulay, exactly
as `Asset_Sourcing.md` § 7.1 asks. **74,170 triangles, 17 materials, its own colours, unmodified.**

⚠️⚠️ **THE FIRST BUILD FOLLOWED § 7.1'S "DECIMATE, MERGE MATERIALS" INSTRUCTION AND WAS REJECTED
ON SIGHT.** 3,000 triangles, one material, UVs rewritten onto the kit's nine-swatch palette atlas
so `tumbang-warm-c` would recolour it like a van. 🧑: *"ew what is that jeep wtf did u do"*,
**"u ate all its colors and design wtf"**, then *"no need to lower triangles or compress dont
worry it wont lag"* and *"make that a rule in claude md"*. **`CLAUDE.md` § 6.0 is that rule.**
Every step was defensible alone; the model was there for its silhouette AND its livery and the
optimisation deleted both, against a frame cost nobody had measured.

- **Placement is still ours**: the model is 24.35 units long as authored, a van is 2.75 drawn at
  1.35, and a jeepney is about 6.0 m against a van's 4.5, so it is drawn at `4.95 / 24.3526`.
  The vehicle table carries a per-row scale now instead of one shared literal.
- **The palette is `""`**, which `InstantiateKitProp` reads as "keep what the author shipped". A
  MISSING atlas still warns, because that is a defect and this is a request.
- ⚠️⚠️ **THE CC BY CREDIT IS ENFORCED RATHER THAN REMEMBERED.** `tools/build_jeepney.py` refuses
  to copy the model unless `CreditsContent.CcByCredits` already names the author, and it reads
  that name out of the .glb's own metadata rather than a constant.

⚠️⚠️ **AND A TRAP WORTH THE LINE: `IlalimNgTulayBuilder.Build` IS A STEP, NOT THE PIPELINE.**
Rebuilding the map through it directly leaves the scene with **no `AsphaltSurface`**, because
that is a separate stage `IlalimNgTulayPipeline` runs after the builder. Nothing says so at the
call site; what says so is `MapSurfaceTests.IlalimUsesOneContinuousAsphaltSkinAndNoPatchSlabs`
going red with *"Expected: 1, But was: 0"*, three Unity launches later. **Use
`IlalimNgTulayPipeline.Run`**, which authors the fascia, builds, lays the asphalt, measures with
`MapGeometryCheck` and captures the showcase in one launch.

**Three more things came off his eye on the renders, and all three are placement rather than
mesh, which is § 6.0's line:**

- ⚠️⚠️ **IT IMPORTS STANDING ON ITS NOSE.** Sketchfab's glTF is authored Z-up with its long axis
  on local +Y, so glTFast brings it in 24 units TALL with its wheels down one side. The vehicle
  row carries a `tilt` of -90 beside its `yaw`, which is the same kind of number the `yaw` column
  already was.
- ⚠️⚠️ **6.0 m WAS TOO SMALL IN THE PICTURE AND RIGHT ON PAPER.** 🧑: *"can u make that bigger
  bcz dude it looks so small compared to cars"*. **The Kenney vehicles are stylised SHORT for
  their width**, so matching real-world proportions against them under-reads. It is 7.5 m now,
  the top of a real jeepney's range, on a street that runs at 0.825 units to the metre.
- ⚠️⚠️ **AND THE METAL FINISH IS PER SURFACE, BECAUSE ONE VALUE FOR EVERYTHING WAS THE FIRST
  ANSWER AND HE NAMED IT: "dont js spam it"**, *"make the white replacement contextual depending
  on which surface of the jeep is affected"*, and *"make sure the parts u paint to look metallic
  make sense"*. **A jeepney's white is at least three materials**: the chrome bumper and grille,
  the painted steel body, and the vinyl bench seats. One 0.70 metal value made the seats look
  like a bumper.

  **The author's own material names are the evidence and the table reads them**, which beats
  guessing from a colour: `silver_shader4Silver_SG` is chrome (0.95 / 0.80),
  `mi_car_paint_phen_x2SG` is a clearcoat over flake and only half metallic (0.40 / 0.65), any
  other bare achromatic panel is a duller 0.55 / 0.35, and `maya_sofa_skin_shadermay` — the
  bench seats, **exactly as white and exactly as untextured as the bodywork** — is refused by
  name along with glass, rubber and plastic. ⚠️ **A TEXTURED material is refused outright**: six
  of the eleven white materials carry the livery under a white base colour, and a sheen on the
  destination boards would be a gloss on the artwork the model is here for. ⚠️ **Anything the
  table does not recognise keeps the material the author shipped**, which is the narrowest thing
  that can be done and the only one that cannot be wrong.

---

### 144.5 ✅ THE LOBBY'S CHARACTERS AND MAP WERE ONE 960 x 540 TEXTURE STRETCHED ACROSS THE SCREEN

🧑 2026-09-04: *"lobby looks so pixelated can u try to fix that too"*, and when asked which part,
**"i meant the characters and stuff"**, *"and the map in lobby"*.

⚠️⚠️ **THOSE ARE NOT TWO FAULTS. THE LOBBY CHARACTER SHOT AND THE MAP SHOT ARE THE SAME
SURFACE**, told apart only by `MapPreviewSurface._lobbyShot`, so one undersized target made both
soft at once. It was a fixed `960 x 540`, with **no anti-aliasing and no filter mode set**, drawn
full-bleed behind the lobby: on a 1440p screen that is a 2.7x upscale of a point-sampled image of
a scene made almost entirely of hard ink outlines.

**The old comment was the whole story:** *"Half the screen is enough behind a scrim, and it halves
the cost."* True of the map shot, which does sit behind a scrim. **The lobby character shot does
not**, and it is the one with faces in it. A number chosen for one caller and inherited by a
second is `CLAUDE.md` § 6.2c's *"what is this size measured AGAINST"* asked about a render target
instead of a rect.

✅ **Fixed, and the framing is provably untouched.** The aspect is now a named constant at 16:9
and the RESOLUTION follows the display between 960 and 2048 wide:

- ⚠️⚠️ **THE ASPECT IS THE FRAMING AND THE RESOLUTION IS NOT.** Every map's `Distance` and
  `Height` was tuned against a 16:9 frame at 58 degrees, and this file already warned that
  changing that ratio *"would silently re-frame all three arenas on the practice screen"*.
  Changing how many pixels the same frame is drawn with re-frames nothing.
- `antiAliasing = 4`, which `ModelPreview` has always asked for and this surface never did.
  `docs/TODO.md` § 63 is the same subject one surface over: the world outline was aliased
  *"because MSAA was never able to see it"*.
- `filterMode = FilterMode.Bilinear`. An unset filter mode takes the project default, and an
  upscaled point-sampled target is the literal definition of the word he used.
- ⚠️ **The camera lets go of the texture before it is released** on a resize. Releasing one a
  camera still points at is a black flash for a frame rather than a crash, which is exactly the
  kind of thing nobody reports precisely.

⚠️ **`ModelPreview` WAS ALREADY CORRECT AND IS THE REFERENCE**: it sizes to its rect, caps at
2048, sets four samples and bilinear, and re-derives the camera aspect. The two files now answer
the same question the same way.

### 144.6 ✅ THE LOGO IS ALREADY THE LOGO, AND THE FILE HE SENT IS BYTE-IDENTICAL TO THE ONE IN THE REPO

🧑 2026-09-04, attaching `logo.jpg`: *"use this as the updated logo for builds (not sure if this
is in repo already)"*, *"make sure its sized correctly and is a png and looks great"*, and
**"dont remake it use the actual photo i have"**.

**It is in the repo, and it is the same file**: `md5 0e46f966…` matches
`Art/ui/brand/source/tump_logo_colour.jpg` exactly. Nothing was regenerated and nothing needed
to be.

- **It is already a PNG where it ships.** `tools/build_brand_art.py` keys the white page to
  alpha and trims the margin: `Art/ui/brand/tump_logo.png` and
  `Resources/UI/brand/tump_logo.png`, **1895 x 1246 RGBA**. ⚠️ **The drawing is untouched** —
  `key_page` and `trim` only remove the paper and the whitespace; `recolour_mono` is a different
  output and does not run on this one.
- **It is used as the hero art on the sign-in/boot screen**, `SignInScreen` loading
  `UI/brand/tump_logo`, and it is what `tools/read_brand_palette.py` measured the whole § 6.4
  palette off.
- ⚠️ **THE BUILD SPLASH CARRIES NO LOGO ON PURPOSE AND THAT IS NOT THIS.**
  `GameBuilder.ConfigureSplash` sets `logos` to an EMPTY array deliberately, and its own note
  says the lookup *"is deleted with the logo"*; the studio mark is shown by `BootSting` in-game
  instead. The `.exe` icon is `app_icon.png`, the tansan. **If either of those should become the
  wordmark, that is a decision rather than a fix**, and nothing here has changed them.

---

### 144.7 ✅ CLOSED 2026-09-05: THE SEAT HANDOVER'S RATING TRAVELS, AND IT COST `ProtocolVersion` 24

`Attention.md` § 16.1 was ruled and not built: *"let ai on same skill level as them take over"*.
Most of it is built now and the remainder is one wire field.

**What landed:**

- `Core/SeatHandover.cs`, engine-free, ten `Core.Tests` cases. `TierFor(rating)` maps a ladder
  number onto the three tiers `AiTuning` actually has, ⚠️ **with both band edges DERIVED**
  (`RatingRules.StartRating` ± `MatchmakingRules.MaxHalfWidth`, so 1000 and 2000) rather than
  picked. That constant already carries the argument in its own words: 500 is *"where banding
  stops meaning anything ... a queue that has widened this far has already said 'skill matching
  has failed'"*. **The distance at which the game refuses to call two players comparable is the
  distance at which it should stop handing their seats the same bot.**
- `SeatOrigin` — `Human`, `Bot`, `HandedToBot` — on `CharacterMotor` and on
  `PlayerMatchStats`, beside `IsBot` rather than replacing it. ⚠️ `IsBot` is on the wire and in
  `IntegrityRules.Digest`; moving it would be a protocol change for a career field.
- **The ladder rule, which is the half with teeth.** § 16.1: *"a bot can lose you points you
  would not have lost, or win you points you did not earn"*, and **"a rating that counts a bot's
  stretch as the player's own is a ladder nobody trusts."** `SeatHandover.RatingWeightFor` is one
  function because it is one rule: my own seat being handed over zeroes my weight, and **somebody
  else's** seat being handed over reduces it, because the match I finished had fewer people in it
  than the one I started. A caller that remembered the first and forgot the second would pay
  three players a four-human result for a match they finished against an AI.
- `AIController.SeatDifficulty`, a per-instance tier defaulting to the lobby's. The difficulty
  was a single static, so **every bot in the game played at one tier** and a handed-over seat
  could only be given the lobby's setting.

⚠️⚠️ **WHAT IS MISSING IS THE RATING, AND § 16.1 SAID SO BEFORE ANY OF THIS WAS WRITTEN:** *"the
game has no notion of 'this player's skill level' to hand the bot"*. Confirmed against the code:
**nothing on `ConnectionHello` or `LobbySession.PeerRecord` carries a rating**, so the host does
not know what tier to ask for. `MatchRpc.RatingForDepartedPeer` is the seam and it answers 0
today, which is the honest answer rather than a stub: the seat keeps the lobby's tier, exactly as
it did before, and **one line changes when the number arrives.**

**Done looks like:** a rating on the connection hello, `RatingForDepartedPeer` reading it, and a
test that a handed-over seat at 2400 gets Astig while one at 700 gets Bata.

✅ **ALL THREE, 2026-09-05.** `ConnectionHello.Rating` carries it, `PeerRecord.Rating` stores it,
`MatchRpc.RatingForDepartedPeer` reads it and `SeatHandover.TierFor` maps it.
`NationalsHardeningTests.ARatingOf2400GetsAstigAnd700GetsBata` is the acceptance test in its own
numbers.

⚠️⚠️ **THE ONE THING THAT WAS NOT OBVIOUS, AND IT WOULD HAVE MADE THE FIELD READ 0 FOR EVERY PEER
THAT EVER COMPLETED A JOIN.** A peer reaches `LobbySession.Admit` **twice**: once from the approval
hello, which is the only message carrying a rating, and again from `MatchRpc.HandleIdentify`. The
second call builds a FRESH `PeerRecord`, so anything `Admit` does not copy forward from the
replaced one is silently zeroed by the peer introducing itself. `Seat`, `Spectator` and the three
picks were already copied; `Rating` had to join them.
`ARatingSurvivesThePeerIntroducingItselfASecondTime` is the regression test, and without it this
whole entry would have shipped looking finished and doing nothing.

⚠️ **IT IS A CLAIM AND NOTHING THAT PAYS OUT READS IT.** A peer types the number it sends, so a
liar can ask for an Astig bot in the seat they are about to abandon. That buys nothing: the ladder
already refuses to move a handed-over seat at all (`SeatHandover.RatingMovesFor`), so the worst a
lie can do is choose which of three difficulties finishes somebody else's match. **Do not widen it
into anything the rating maths reads.**

⚠️ **0 STILL MEANS "DID NOT SAY" AND LEAVES THE LOBBY'S TIER ALONE**, which is what a LAN guest
with no career and a peer that never signed in both produce. Inventing a mid-ladder guess for them
would hand a stranger's chair a bot matched to a number nobody measured.

⚠️⚠️ **AND IT COSTS A PROTOCOL BUMP, WHICH IS WHY IT WAS NOT SLIPPED INTO THIS SESSION'S BUILD.**
A field on the hello moves `NetSession.ProtocolVersion`, and `CLAUDE.md` § 4 is explicit about
what follows: *"the Windows and Android players must be rebuilt from the same commit and shipped
together, or they refuse each other correctly and it reads as a bug"*.
`InputContractTests.TheInputPassDidNotMoveTheProtocolVersion` asserts the constant so that a
legitimate bump is a deliberate act. **Do it at the start of a session, not at the end of one.**

---

### 144.8 ⚠️⚠️ OPEN, AND HANDED TO ANOTHER SESSION: THE JEEPNEY'S METAL FINISH DOES NOT SHOW

🧑, on the fourth render, with a close crop attached: **"this js white shit gang idk if u did
shaders properly o rnot"**, then *"anyways lets js get diff chat to do it"*. **So this is
somebody else's to pick up, and it is written down rather than half-fixed.**

**What is in the code right now** (`IlalimNgTulayBuilder.GiveWhitePanelsAMetalFinish` and
`TryFinishFor`): a per-surface table that copies each of the jeepney's materials and writes
`_Metallic` and `_Smoothness` onto the copy. Chrome 0.95/0.80 for the material the author named
`silver_shader4Silver_SG`, 0.40/0.65 for `mi_car_paint_phen_x2SG`, 0.55/0.35 for any other bare
achromatic panel, and a by-name refusal for the bench seats, glass, rubber and plastic. **The
selection logic is sound and was checked against the model's own seventeen material names.** It
is the RESULT that does not read: the body is as flat white in the render as it was before.

⚠️⚠️ **DO NOT START BY RETUNING THE NUMBERS. THE FIRST STEP IS ONE MEASUREMENT AND IT HAS NOT
BEEN TAKEN.** Raising 0.40 to 0.9 is the obvious move and it is worthless if the write is not
landing at all. **Print, for each renderer on the placed jeepney: the material name, the SHADER
name, and the values `_Metallic` and `_Smoothness` actually hold after the pass runs.** That one
table tells you which of these it is, and they need completely different fixes:

1. ⚠️ **THE PROPERTY NAMES ARE WRONG FOR THE SHADER glTFast BUILT.** `HasProperty` returns false
   and both writes are silently skipped, which is a no-op that logs nothing. URP Lit uses
   `_Metallic` and `_Smoothness`; **glTFast's own `glTF/PbrMetallicRoughness` shader does not**,
   and which one an import produces depends on the package's render-pipeline detection. This is
   the most likely answer and it is also the cheapest to confirm.
2. ⚠️ **THE WRITE LANDS AND THERE IS NOTHING TO REFLECT.** Metal is reflection: a metallic
   surface with no environment probe and no reflection source renders as a dark or flat patch,
   not as shine. The preview and the map both draw with a skybox, so this is less likely than
   (1), but `Assets/TumbangPreso/Scenes/Maps/IlalimNgTulay.unity` should be checked for a
   reflection probe over the north boundary before any number is touched.
3. ⚠️ **THE MATERIAL IS AN INSTANCE THE SCENE DID NOT KEEP.** `sharedMaterials` is written on a
   `PrefabUtility.InstantiatePrefab` instance, which is a prefab override; if the scene save
   drops it the finish exists at build time and not at play time.

⚠️ **AND A FOURTH POSSIBILITY WORTH RULING OUT LAST: it may be landing and simply not reading at
that distance under this map's flat lighting**, in which case the honest answer is a stronger
contrast between the chrome and the paint rather than more metal on everything, which is the
*"dont js spam it"* note again.

⚠️ **THE SELECTION IS THE PART WORTH KEEPING.** Whatever the fix turns out to be, the rules that
decide WHICH surfaces get it were checked against the model and are the answer to *"make sure the
parts u paint to look metallic make sense"*: textured materials are the livery and are refused
outright, `maya_sofa_skin_shadermay` is the bench seats and is refused by name, and anything with
a hue is his paint. **Do not replace that with "every white material".**

#### ✅ THE MEASUREMENT WAS TAKEN, 2026-09-05, AND IT IS CAUSES 1 AND 2 TOGETHER

⚠️⚠️ **`JeepneyFinishProbe` WAS ADDED BY `e49bd2b` AND HAD NEVER BEEN RUN.** It is exactly the
table this entry demands and it was sitting in the repository unexecuted, which is why this row
still read *"the first step is one measurement and it has not been taken"*. `Logs/jeepney-finish.txt`
is the output.

**Cause 1: CONFIRMED, and it is the whole of the "nothing happened" half.**

| | |
|---|---|
| Materials on the placed prop | **17**, every one named `..._finish` |
| Shader, on all 17 | **`glTF/PbrMetallicRoughness`** |
| `HasProperty("_Metallic")` | **False on all 17** |
| `HasProperty("_Smoothness")` | **False on all 17** |

**So both writes were silently skipped on every surface**, exactly as this entry predicted was
most likely. ⚠️ **Cause 3 is RULED OUT by the same table**: the `_finish` suffix on all 17 says the
pass ran, made its copies, and the scene kept them. The selection logic was never the problem and
the numbers were never the problem.

⚠️⚠️ **AND THE PROBE NOW PRINTS WHAT TO WRITE INSTEAD, BECAUSE "THE NAMES ARE WRONG" DOES NOT SAY
WHAT THE RIGHT ONES ARE AND GUESSING THEM IS THE THING THIS PROBE EXISTS TO REPLACE.** glTFast's
property names have moved across versions of that package, so the shader is asked. It declares:

```
roughnessFactor       Range
metallicFactor        Range
baseColorFactor       Color
```

⚠️⚠️ **`roughnessFactor` IS THE INVERSE OF SMOOTHNESS AND THE NUMBERS MUST NOT BE TRANSCRIBED.**
The chrome's authored 0.80 smoothness is **0.20 roughness**; copying the existing table across
would make the shiniest surface on the jeepney the dullest one.

**Cause 2: ALSO CONFIRMED, and it is the "still will not show" half.**

> `reflection probes in the scene: 0`

⚠️⚠️ **SO FIXING THE PROPERTY NAMES ALONE WILL NOT FINISH THIS.** Metal is reflection: a metallic
surface with nothing to reflect renders as a flat or dark patch whatever `metallicFactor` says.
This entry guessed cause 2 was *"less likely than (1)"* because the map draws with a skybox; the
measurement says **both are true at once**, and a skybox alone gives a mirror-flat ambient rather
than the body panel and window shapes that read as chrome. `IlalimNgTulay.unity` needs a
reflection probe over the north boundary before the finish can be judged at all.

⚠️ **THE FOURTH POSSIBILITY IS STILL UNJUDGED AND IS STILL LAST.** Whether it READS at that
distance under this map's flat lighting is a question for a render and an eye, and it cannot be
asked until 1 and 2 are both fixed. **Do not retune a number before then**, which is what this
entry has said from the start.

---

### 143.15 ✅ CLOSED 2026-09-05: THE COLD START RAN, AND THE FIRST RUN FOUND A HOLE IN THE HARNESS

`python tools/cold_start.py --clean-profile`, against a player built from HEAD:

```
Verdict: PASS
launches and identifies itself            PASS   3.4 s
hosts a match with four bots and finishes PASS  48.0 s
```

`docs/reports/cold-start-28078b66614d.md` is the artifact.

⚠️⚠️ **THE REFUSAL FIRED FIRST AND THAT IS WORTH RECORDING AS A PASS OF ITS OWN.** The first
attempt came back *"REFUSED: the player was built from 41217d83bc20 and HEAD is 28078b66614d. A
cold start of a different commit proves nothing about this one."* The build was one docs-only
commit behind, which is exactly the reasoning the guard exists to refuse, and it refused it. The
player was rebuilt from HEAD and the run means something.

⚠️⚠️ **AND THE GREEN RUN'S OWN REPORT CONTRADICTS ITS SECOND STEP, WHICH IS A HOLE IN THIS
HARNESS AND NOT IN THE GAME.** The step reads *"hosts a match with four bots and finishes:
PASS"*, and the state it captured reads:

```
round           : 0
round active    : False
seat 0 travelled 20.8   seat 1 travelled 17.4   seat 2 travelled 10.2   seat 3 travelled 15.4
```

**No round ran.** The bodies moved, so the arena installed and the bots are driving, but the
match never started. `tools/net_matrix.py` records this exact trap in its own source, in
capitals: *"`-tp-autostart 2` IS NOT OPTIONAL AND ITS ABSENCE IS SILENT. `-tp-host` loads the
arena, but `MatchInstaller.BuildReadyGate` opens a ready gate on any NETWORKED session, and
nothing presses through it without this switch ... Two peers agreeing that a round never started
is not evidence about the link."* **The same sentence is true of one peer agreeing with itself.**

⚠️ **WHAT THE RUN DOES PROVE**, and it is not nothing: a player from a stamped commit launches on
a cleared profile, identifies itself, reaches the arena, installs four bots that move, holds for
45 seconds and exits cleanly, on a machine that has never run this build. **What it does not
prove is that a match plays**, which is what the step's wording claims. Fixing it is one switch
and one assertion on `round active`. § 143.15b in the queue.

⚠️ **A truly clean MACHINE is still `Attention.md`.** This clears a profile; it cannot clear a
driver, a firewall rule or a runtime that this machine has and a borrowed one does not.

### 143.15b ✅ CLOSED 2026-09-05: THE STEP ASSERTS A ROUND RAN, AND IT IS A SEPARATE ROW NOW

Two fixes, and the second is the one worth reading.

1. **`-tp-autostart 1`.** `MatchInstaller.BuildReadyGate` opens a ready gate on any NETWORKED
   session and nothing presses through it without the switch, which is the trap `net_matrix`
   already records in capitals. The count is **1** rather than 2 because
   `LobbySession.PlayingPeerCount` counts SEATED peers and a solo all-bots host is one of them.
2. ⚠️⚠️ **THE REPORT NOW HAS TWO ROWS WHERE IT HAD ONE, AND CONFLATING THEM WAS THE WHOLE FAULT.**
   *"Reaches the arena and exits cleanly"* is a claim about the PROCESS: it launched from a
   cleared profile, identified itself, loaded a map, installed four bots and came back. *"A real
   round became active"* is a claim about the GAME. Every assertion the old step made was the
   first one, printed under the second one's name.

**MEASURED on `87346b8`**, `python tools/cold_start.py --seconds 50` against a player built from
that commit:

```
| launches and identifies itself      | PASS |  2.8 | clean |
| reaches the arena and exits cleanly | PASS | 51.2 | clean |
| a real round became active          | PASS |  0.0 | round 1, active, 4 seats driving, 50.0 s |
```

and the capture underneath it now reads `round: 1` / `round active: True` with a scoreboard on it
(1370 points, six lata flips, all four seats between 56 and 94 m travelled) where § 143.15's green
run read `round: 0` / `round active: False`. **That is the difference the switch makes**, and it
was invisible for as long as the step asserted only that the process came back.

**What the new step asserts:** the report exists, `round >= 1`, `round active` is True, the
session was networked (the gate the switch presses only exists on one), and **at least two seats
travelled more than a metre**, because "a round is active" and "anybody is playing" are also two
claims, and four seats standing still inside a live round is `-tp-allbots` not having taken.

⚠️ **AND THE HARNESS RUNS ON THIS MACHINE NOW.** It was Windows-only, `TumbangPreso.exe` on the
Desktop, `AppData/LocalLow` for the profile, so on the Mac in `CLAUDE.md` § 7's table it could
only ever refuse, which is that section's own *"true on one machine and written as a fact about
'here'"* warning landing on a tool. It finds the macOS bundle (`Contents/MacOS/TumbangPreso`, and
the stamp under `Contents/Resources/Data/StreamingAssets`) and
`~/Library/Application Support/BH Studios/Tumbang Preso`.

⚠️⚠️ **`--clean-profile` MOVES A DIRECTORY, SO GUESSING ITS PATH WRONG IS DESTRUCTIVE**, which is
why the three platform layouts are written out rather than derived from one pattern.

⚠️⚠️⚠️ **AND THE RUN AFTER THAT DELETED A REAL PROFILE DIRECTORY, WHICH IS THE MOST IMPORTANT
THING IN THIS SECTION.** The new step bound its per-seat list to a local called `moved`:

```python
moved = [s for s in state.get("seats", []) if s["travelled"] > 1.0]
```

`moved` is the `finally` block's handle for *"a profile directory was set aside and has to be put
back"*. With a list bound to it, that block read "a backup exists", ran **`shutil.rmtree(profile)`
on a profile it had never moved**, and only THEN failed trying to restore a Python list. The run
was not even given `--clean-profile`. `~/Library/Application Support/BH Studios/Tumbang Preso` was
destroyed.

⚠️⚠️ **THE DESTRUCTIVE HALF RAN FIRST, AND THAT IS THE PROPERTY TO LEARN FROM RATHER THAN THE
TYPO.** An unguarded `rmtree` inside a `finally`, conditioned on a FLAG about a backup rather than
on the backup, is a bad shape however careful the surrounding code is: any path that leaves that
flag truthy deletes first and discovers the mistake afterwards. So the condition is now the thing
itself: a real `pathlib.Path`, equal to the name this function chose, that exists on disk. Anything
else prints `REFUSED to restore` and deletes nothing. **Verified by binding the exact
list that caused the loss and watching the guard fire with the file intact.**

⚠️ **AND THE RENAME TO `driving` ONLY FIXES THE INSTANCE.** The guard is what fixes the class,
which is `CLAUDE.md` § 4a's construction argument applied to a destructive operation.

⚠️⚠️ **AND THE FIRST CROSS-PLATFORM RUN STILL REFUSED, ON A NAME.** The binary inside a `.app` is
named after `productName`, not after the bundle: `ProjectSettings.asset` says
`productName: Tumbang Preso`, **with a space**, so the executable is
`Contents/MacOS/Tumbang Preso` inside `TumbangPreso.app`. The harness looked for
`Contents/MacOS/TumbangPreso` and reported *"there is no shipped player on this machine"* at a
player that had just built successfully. **It globs the bundle now**, so a `productName` change
cannot break it, which is `InputSurfaceProbe`'s discover-rather-than-list argument applied to a
path.

---

## 143 · THE NATIONALS HARDENING PASS: A QUALIFICATION THAT CANNOT LIE ⚠️⚠️ IN PROGRESS, 2026-09-04, branch `main`

⚠️⚠️ **THIS WAS WRITTEN AS § 142 AND WAS RENUMBERED ON MERGE, WHICH THIS FILE NORMALLY REFUSES TO
DO.** `controller-mapping` and this pass were both written on 2026-09-04, both off `e85b0fc`, and
both claimed **§ 142**. The rule in "How this file stays short" is that duplicate top-level numbers
are tolerated because renumbering breaks pointers, and that rule is about numbers which already
disagree in the archive. **This was different in the one way that matters: both sections had a
§ 142.1 and they meant completely different things** (this one is the PlayMode suite not being a
gate; the controller one is every back-out being keyboard-only). A source comment reading
`docs/TODO.md § 142.1` would have been genuinely ambiguous rather than merely duplicated.

**The hardening pass moved because it was cheaper to move**: six references in four files this
session wrote, against twelve across seven for the controller pass, including `CLAUDE.md` and
`Attention.md`. Nothing else was renumbered and the controller section keeps § 142 whole.


**Nationals is about three months out. This entry is not a bug list; it is the pass that makes the
game hard to break, hard to ship wrongly, and easy to diagnose in a hall.** Every subsection below
carries its own measurement taken on `e85b0fc`, because the thing this pass is mostly fighting is
numbers quoted from a run nobody can tie to a commit.

⚠️⚠️ **THE ORGANISING FINDING, AND IT IS THE SAME ONE § 126.8 FOUND THREE TIMES: A GREEN SUBSET IS
NOT A RELEASE CERTIFICATION.** Every PlayMode number quoted in every handoff in this file came from
a targeted run. The full suite has been 42 red, then 41, then 56, and now 50, and the RED SET MOVES
between runs on unchanged code. A gate whose red set moves is not measuring the code.

### 143.1 ⚠️⚠️ OPEN: THE BASELINE ON `e85b0fc`, AND WHY "RUN IT TWICE" WOULD NOT HAVE HELPED

**One full PlayMode run, `-buildTarget Win64`, the shipped exclusions, `Logs/play-baseline.xml`:**

```
165 cases, 107 passed, 50 failed, 8 skipped, 619 s
```

⚠️⚠️ **THE HANDOFF THAT COMMISSIONED THIS PASS ASKED FOR THE FULL SUITE TO PASS TWICE BACK TO BACK
AS A NATIONALS GATE. THAT WOULD HAVE BEEN THE WRONG GATE, AND THE EVIDENCE IS IN THE RUN ITSELF.**
Two things in the failure list settle it:

1. **`SettingsScrollProbe.TheSettingsListScrollsAndItsBarCoversNothing` failed with the message
   *"a held slipper drifted 7.945 m from the hand while its carrier walked"*.** That is
   `CarryTests`' assertion, reported against a settings test. **A suite that attributes one
   fixture's failure to another fixture is not measuring either of them.** (It is also a 7.945 m
   drift against a 0.05 m bound, where § 93's real, isolated samples are 0.084 to 0.092 m: the
   number itself is a leaked object, not a carry bug.)
2. **Twelve fixtures died on `MissingReferenceException: the object has been destroyed`**, and nine
   more reported that a screen or an arena *"was never built"*: *"MatchSetup has no
   CharacterSelectPanel to open"* (7 cases), *"the arena built no SliceRunner"* (2), *"the lobby
   must have a LobbyChat"*, *"no EventSystem"*, *"the sign-in screen: nothing was built, so this
   proves nothing"*.

**Running that twice produces the same contamination twice and calls it reproducible.**

⚠️ **§ 126.8 ALREADY NAMED THE TWO WAYS OUT AND BUILT NEITHER**: every fixture tears its world down
(attempted; § 126.8d measured it moving eleven failures from one side to the other and **withdrew**
it), or **the suite is declared to run in named groups and a single-process full run stops being
quoted as a gate at all.** This pass takes the second, because § 126.8d's own measurement is the
argument against the first: *"what the right version needs is a measurement nobody has taken: WHICH
persistent object a match install depends on."* Grouping does not need that measurement. It removes
the question.

**Done looks like** `tools/playmode_suite.py --gate`: fixtures partitioned into groups that cannot
reach each other, one Unity launch per group, results aggregated into one verdict, **and coverage
asserted so a group that silently ran nothing fails instead of passing.** Green twice.

✅ **THE EXPERIMENT IS DONE AND THE THESIS HOLDS.** The `screens` group, 26 fixtures, run alone:

```
64 cases, 51 passed, 13 failed, 106 s      (Logs/playmode-suite/screens.xml)
```

**Against the same fixtures inside the full run: about thirty failures, seven of them the identical
sentence *"MatchSetup has no CharacterSelectPanel to open"* and five more `MissingReferenceException`.
In isolation there is not one `MissingReferenceException` in the group**, and the seven identical
phantoms became ONE specific finding: *"no 'LoadoutDoor' on the character select stage"*.

⚠️⚠️ **THAT IS THE ARGUMENT IN ONE LINE: ISOLATION DID NOT MAKE FAILURES GO AWAY, IT TURNED NOISE
INTO SIGNAL.** The 13 that remain name real defects and two of them were previously invisible
because a phantom was sitting on top of them (§ 143.17).

⚠️ **The partition is DISCOVERED against the source, not listed.** `--plan` refuses to run at all
if any fixture is in no group or in two, and the first run of that check earned its keep
immediately: the discovery regex required a bare `[UnityTest]` and `BotBehaviourProbe` writes
`[UnityTest, Timeout(MatchTimeoutMs)]`, so the gate would have silently covered 67 fixtures of 68
and dropped the longest probe in the suite.

### 143.1a ✅ THE WHOLE GATE RAN ON `0ae070e`, AND THE COMPARISON IS THE ENTRY

```
one process,  e85b0fc :  165 cases, 107 passed, 50 failed
six groups,   0ae070e :  175 cases, 150 passed, 17 failed, 8 skipped
```

| Group | Cases | Failed | What is left |
|---|---|---|---|
| `destroyer` | 5 | **1** | a real focus-path finding on Eskinita's result card |
| `screens` | 65 | **12** | six are one cause (§ 143.18); the rest are layout and a lost control |
| `match` | 73 | **4** | § 93's carry drift and three `SteeringTests` settle failures |
| `capture` | 10 | **0** | green |
| `services` | 19 | **0** | 8 correctly skipped: UGS sign-in is off in batch mode |
| `bots` | 3 | **0** | green |

⚠️⚠️ **IT RUNS MORE CASES AND FINDS FEWER FAILURES, WHICH IS THE WHOLE ARGUMENT IN ONE LINE.**
175 against 165: the extra ten are cases that previously never executed because a fixture died on
a leaked object before reaching them. **Isolation did not hide failures, it stopped fixtures being
blamed for each other's leaks**, and the two that were invisible underneath a phantom are § 143.18.

**Two failures from the full run vanished in isolation and both were about the match itself:**
`MatchRunTests.AWholeMatchRunsAndRotatesTheTaya` (*"seat 1 never defended: the rotation is
broken"*) and `RestoredLataRejectsAnAlreadyAirborneFollowUp` (*"restore protection never
expired"*). **Both pass alone.** A gate reporting that the taya rotation is broken, four runs
running, when it is not, is worse than no gate: it is the reason nobody believed the suite.

⚠️ **`CarryTests` reports exactly 0.084 m in isolation**, which is § 93's first recorded sample.
In the full run the same bound failed at **7.945 m**, on a SETTINGS test. The isolated number is
the real one.

### 143.1b ⚠️⚠️ THE COVERAGE CHECK'S FIRST RUN FAILED THE GATE OVER SEVEN FIXTURES THAT WERE MEANT TO BE SILENT

**Worth recording because the fix is the difference between a gate and a nuisance.** The first
full run reported *"7 fixture(s) never ran"* and refused, and every one of the seven is
`[Category("WallClock")]`, which the shipped filter excludes on purpose: `AiDiagnosticProbe` runs
a round at 1x for about eighty real seconds and its verdict depends on how busy the machine is
(`CLAUDE.md` § 7), and the other six photograph things at real time for the same reason.

⚠️⚠️ **A GATE THAT CANNOT BE GREEN IS A GATE THAT GETS IGNORED**, which is the failure this whole
file exists to prevent, so the check now derives which fixtures the active filter legitimately
silences and reports them separately from ones that went missing. ⚠️ **It is derived from the
source and not listed**, for the same reason the partition is: a hand-written exclusion list stops
being true the moment somebody adds a category, and it fails in the direction that hides a
genuinely missing fixture.

**Three parsing faults had to be fixed before it saw all seven**, and each is a small lesson:

- The category sits on the line **after** `[UnityTest]`, not before, so a window of preceding
  lines found one fixture of the seven. It reads the whole contiguous attribute block now.
- `AiDiagnosticProbe` writes `[Category(WallClock)]` against a **const rather than a string
  literal**, and a regex that only knew the quoted form called it missing.
- `MsaaResolveProbe`'s class note contains the words *"A `[UnityTest]` coroutine resumes"*, and
  counting that as a case made the fixture look like it had an unexcluded test. **Comments are
  stripped first now**, which is `audit_audio_reach.py`'s lesson exactly: it *"was the only audit
  that did not strip comments before looking for a gate"*, so a comment ABOUT a gate registered
  as one.

⚠️⚠️ **AND IT MUST NOT BECOME A THIRD CATEGORY EXCLUSION.** § 126.8d bans that explicitly: a
category meaning *"these tests do not work next to each other"* hides this finding rather than
recording it. **A group is an isolation boundary, not an exemption: every fixture still runs, in
exactly one group, and the aggregate is the number quoted.**

### 143.2 ⚠️ OPEN: A BUILD CANNOT SAY WHAT IT IS

`GameVersion.DisplayString` prints `v1.00` in the corner and `BuildBranch` stamps the branch, and
neither answers the question an operator actually has at a venue, which is **"are these two
machines running the same game"**.

- `NetSession.ProtocolVersion` is **23** on this commit, read from
  `Assets/TumbangPreso/Runtime/Net/NetSession.cs`. ⚠️ **It is read from source by
  `tools/qualify.py` and copied into no document**; this line is a measurement with a date on it,
  not a second source of truth. The preamble to this file has gone stale on this number four times.
- **Peers on different protocol numbers refuse each other by design.** A Windows player and an
  .apk built from different commits therefore fail in a way that reads exactly like a network bug,
  and `Attention.md` § 1 already warns a human about it in prose. Prose is not a gate.

**Done looks like** a `build-identity.json` emitted into both players by `GameBuilder`, a
diagnostic route that prints it without opening source, and `tools/qualify.py --stage identity`
refusing a pair that disagree on SHA or protocol.

### 143.3 ⚠️ IN PROGRESS: THE TOURNAMENT PRESET

✅ **The rules half landed 2026-09-04.** `Packages/com.tumbangpreso.core/Runtime/TournamentPreset.cs`
is the single answer to "what is a nationals match", and it copies no number: `Rounds` asks
`MatchRules.RoundCountFor`, `RoundSeconds` asks `Balance.RoundTime`, `Tsinelas` asks
`CustomGameRules.StartingTsinelas`. `docs/VISION.md` § 1.1's *"CLASSIC IS THE TOURNAMENT RULESET"*
is a constant now, so changing the ruling fails a test rather than being an argument.

⚠️⚠️ **THE HAZARD IT REMOVES IS REAL AND IS ASSERTED: `new CustomRules()` IS HERO STRIKE, EIGHT
ROUNDS.** The field initialisers say so, correctly for that class, and any start path that builds a
bare rule set is therefore a Hero Strike match wearing a Classic tournament's name.
`TournamentPresetTests.ABareCustomRulesIsNotATournamentMatchAndThatIsWhyThePresetExists`.

**`TournamentPreset.Modifiers` is the deliverable half**: eight named switches, each with the reason
it is on the list, because the NAME is what gets forgotten when somebody invents switch number nine.
⚠️ **One row's safe value is `true`** (`AIController.BotsEnabled`): turning bots off does not make a
match more human, it makes unfilled seats inert.

⚠️ **The stale claim is confirmed stale.** `PracticeSandbox` has no `const bool NoCooldowns = true`
and never reaches a networked match: `Allowed => !NetAuthority.IsNetworked` is asked every frame
rather than latched, so a sandbox left on in a solo match stops answering true the moment a session
exists. **It is on the modifier list anyway**, because the guard is the thing under test and because
a lit NO COOLDOWNS toggle in a tournament room is a HUD disagreeing with the game.

**Open: the Unity half**, which reads the eight live values and refuses.

### 143.4 ✅ CLOSED 2026-09-04: THE SOAK HARNESS, AND WHAT SIX MATCHES MEASURED

`MatchSoakProbe` runs `build -> match -> teardown -> rematch` six times in one process,
**alternating Classic and Hero Strike every iteration** so the pass also crosses the boundary a
bracket day actually crosses: finish one format, start another, same process.

```
6 matches, 0 exceptions, 0 invariant violations, 0 leaked CharacterMotors
managed memory 625.72 MB -> 625.91 MB  (+0.19 MB across six matches)
live GameObjects 11, 11, 13, 13, 13, 13
rounds 4, 8, 4, 8, 4, 8   (the tournament preset really is producing four-round Classic)
```

⚠️⚠️ **THE HARNESS'S FIRST RUN ACCUSED THE GAME OF TWO THINGS THAT WERE THE HARNESS'S OWN FAULT,
AND BOTH ARE WORTH RECORDING BECAUSE THEY ARE THE SHAPE OF EVERY BAD PROBE.**

1. *"seat 0 began iteration 1 holding 20 points from a previous match"* on **iteration 1**, where
   there is no previous match. It read the scoreboard one frame AFTER the runner began, and at
   60x one frame is about a second of game time, so it measured two defence ticks.
2. *"seat 0 moved by 70 points, which is not any ScoreEvent's value"*. Also legitimate: seven
   defence ticks in one sampled frame. **`IsReachableDelta` allowed at most two awards per step
   because a network snapshot pair spans 200 ms at 5 Hz, and that bound belonged to the OBSERVER
   rather than to the rule.** It takes a `maxEvents` now, the wire still passes 2, and the soak
   derives its allowance from the game time that actually elapsed.

⚠️⚠️ **AND WHAT IT IS NOT IS A LIVENESS MEASUREMENT.** Every seat finished on an exact multiple
of `ScoreDefensePerTick`: 900 in a four-round Classic match is 90 defence ticks and nothing else,
which is a whole match where the lata was never knocked over. **At `Time.timeScale = 60` the bots
effectively do not play**, which is why `BotBehaviourProbe` moved to a fixed 1/60 s step. Do not
read a score out of `Logs/soak.json` as balance evidence. **It does not weaken the soak**: a quiet
match crosses the same boundaries as a busy one, and boundaries are the whole subject.

### 143.4b ⚠️ THE ORIGINAL STATEMENT OF THE PROBLEM, KEPT

`BotBehaviourProbe` runs a match. `MatchRunTests` runs a match. `GameplayShots` photographs a match.
**Nothing anywhere runs the fifth match after the fourth rematch**, which is the shape of a
tournament afternoon and the shape of every accumulated-state defect.

**Done looks like** a soak harness cycling `launch → lobby → match → results → rematch → lobby →
match`, watching exceptions, assertion failures, memory growth, GC pressure, stuck rounds and
timers, duplicate scores, duplicate event callbacks, seat ownership corruption, replay buffer
growth, static leakage and host/client divergence, writing a machine-readable summary tied to the
SHA.

### 143.5 ✅ CLOSED 2026-09-04: THE SUBSCRIPTION AUDIT, AND IT FOUND A REAL CROSS-MATCH LEAK

`tools/audit_event_subscriptions.py` pairs every `+=` with a `-=`. **85 subscriptions in
`Runtime/`, and one file was leaking four of them into the next match.**

⚠️⚠️ **`MatchBootstrap` SUBSCRIBED FOUR HANDLERS TO A `DontDestroyOnLoad` DIRECTOR AND REMOVED
NONE.** `GameServices` is the process-lifetime service root, so `MatchDirector` outlives every
arena; `MatchBootstrap` lives in the arena scene. What that cost:

- **The arena unloads, the component is destroyed, and the four handlers stay registered.** The
  next match therefore runs `OnRoundStarted` on a destroyed `MatchBootstrap`, and that handler
  calls `ResetWorld`, **which teleports all four bodies and hands out the tsinelas.** Match five
  was running it five times.
- **`BuildAndStart` is public**, so a second call subscribed a second copy of every handler to the
  same event on the same object.
- **And the pending `Invoke` was the same leak in a second shape.** `OnIntermission` schedules
  `AdvanceAfterIntermission` on this component, and an `Invoke` outliving its target calls
  `GameServices.Match.AdvanceRound()` on a director that has moved on. **A round advanced by the
  previous match's timer** is `VISION.md` § 4's first rule broken from outside the match.

⚠️ **None of that crashes**, which is why it survived every test in the repository. It is a round
that resets more than once, and it reads as "the game got weird after a few matches".

✅ Fixed: the director is cached in `_hookedMatch` (the pattern `AIController` already used and this
file did not), `Subscribe` releases before it takes so no path can leave two, and `OnDestroy`
cancels the invoke and unsubscribes.

⚠️⚠️ **TWO FALSE-POSITIVE CLASSES HAD TO BE REMOVED BEFORE THE AUDIT WAS WORTH READING, AND BOTH
ARE RECORDED IN THE FILE.** Its first run reported 76 findings, about sixty of which were
`_clock += dt`: `+=` is arithmetic and subscription with identical syntax. Its second reported
`AIController`'s five CORRECT unsubscribes as leaks, because that file releases through
`_hookedMatch.Scored` rather than `match.Scored`, **and caching the exact publisher is the right
pattern rather than sloppiness.** An audit that punishes the correct pattern is an audit somebody
switches off.

### 143.6 ✅ CLOSED 2026-09-04: `SceneDependencyCheck`, THE OPPOSITE TECHNIQUE TO `SceneScriptCheck`

`SceneScriptCheck` reads scenes as TEXT **on purpose**, because the fault it hunts is one the
editor resolves by class name and the player cannot, so opening the scene is what hides it. The
faults a human finds by opening a build are the mirror image: a reference pointing at a deleted
object, a component whose script is gone, a scene that will not open at all. **Neither technique
can see the other's defect**, so this is a second check rather than an extension of the first, and
`Checks.RunAll` is eight checks now.

```
9 build scenes, 11536 components, 0 findings      (Logs/scene-dependency-check.txt)
```

⚠️ **The missing-reference test is `objectReferenceValue == null` AND a non-zero entity id**, and
that distinction is what gives it no false positives: a never-assigned field is ordinary and an
optional hook is ordinary, while a field pointing at an id that no longer resolves is never
correct. ⚠️ `objectReferenceInstanceIDValue` is **obsolete as an ERROR** in Unity 6, not merely
deprecated; the editor assembly refuses to compile against it.

⚠️⚠️ **AND THE TWO REQUIREMENTS THIS CHECK ORIGINALLY SHIPPED WERE BOTH WRONG, WHICH IS RECORDED
IN THE FILE RATHER THAN QUIETLY FIXED.** Both looked obviously right and both asserted something
the code does not do:

- **A camera in every arena.** All three maps failed. A map authors no camera because
  `MatchInstaller` BUILDS the rig at runtime, per seat, and `CLAUDE.md` § 4 is why (FPP for a
  Person, TPP for a Prop, which cannot be authored before anybody is sitting anywhere).
- **`Spawn0` to `Spawn3`, and `Floor`.** The maps do author spawn markers, and **nothing reads
  them**: every seat is placed from `Confinement.AttackerSpawnRing()`. The floor is called `Floor`
  on two maps and `AsphaltSurface` on Ilalim ng Tulay, and `MapGeometryCheck` already owns the
  real property (holes, floating props, furniture inside the box).

**A check that asserts a convention rather than a contract goes red on correct work, and a check
that goes red on correct work gets deleted along with whatever it was protecting.**

### 143.7 ⚠️ OPEN: THE AUTHORITATIVE PATHS ARE ARGUED, NOT TESTED, AGAINST DUPLICATES

The architecture is right and the audits say so on this commit:

| Audit | Reading on `e85b0fc` |
|---|---|
| `audit_ability_authority.py` | **49** effect call sites, 30 host-gated, **0 ungated on another body**, 19 ungated on the caster |
| `audit_request_call_sites.py` | **59** wire entry points, **0 unreachable** |
| `audit_wire_payloads.py` | **61** named messages, **0 mismatched** |

⚠️ **That is an argument about shape, and a duplicate is not a shape problem.** A replayed
`SubmitScoreServerRpc`-shaped request is well formed, correctly gated, from a legitimate peer, and
awards twice. Nothing tests that.

✅ **The engine-free half landed 2026-09-04.** `MatchInvariants.IsReachableDelta` states the rule:
the only legal score movements are sums of at most two `ScoreEvent` values, **so 300 where the
event pays 100 is not a bigger award, it is three awards.**

### 143.8 ✅ THE AUDITS ARE A GATE NOW

`tools/qualify.py --stage audits` runs all of them, exits non-zero on any finding, and the verdict
lands in the qualification report. ⚠️ `PYTHONIOENCODING=utf-8` is set by the runner rather than
being remembered, because `audit_audio_reach.py` dies on a `UnicodeEncodeError` without it and the
crash looks like a fault in the thing it is auditing.

### 143.9 ✅ CLOSED 2026-09-05: HOST LOSS IS ONE OUTCOME ON EVERY PEER, AND ONE EXPRESSION WAS THE HOLE

`_utp.DisconnectTimeoutMS = 8000` (`NetSession.cs:1172`), so a peer whose wifi dies keeps a
normal-looking arena for eight seconds. § 140 is the player-facing half and is still open.

⚠️⚠️ **AND THE EIGHT SECONDS WERE NEVER THE DANGEROUS PART. THIS WAS:**

```csharp
public bool IsHost => _nm == null || !_nm.IsListening || _nm.IsServer;
```

Every clause is right for the case it was written for, no transport is the offline game, a server
is a host. The state it does **not** describe is a CLIENT whose transport has just stopped,
which satisfies the middle clause. `NetAuthority.ShouldResolve()` was exactly `IsHost`. **So the
moment a host disappears, every client in the room starts answering "I decide outcomes":** it
resolves its own tags, awards its own points and advances its own rounds, in an arena nobody else
is in. Four peers that were obeying one referee become four referees.

⚠️ **IT WAS ALREADY WRITTEN DOWN FROM THE OTHER DIRECTION AND NOBODY JOINED THE TWO.**
`MatchRpc.HandleClientDisconnected` carries a capitalised note saying an `IsHost` guard there broke
the handler because *"it answers TRUE the moment the transport stops listening, which is precisely
the state a peer is in while it is being disconnected."* That is this defect, observed, one file
away, months earlier.

**What landed:**

- `Core/SessionEnd.cs`: one closed set of causes, one classifier, and `RevokesAuthority`.
  ⚠️ **Wide on purpose**: everything except a local quit revokes, because a peer removed by the
  host, one refused for its protocol and one whose host vanished are in **identical local state**
  and any of them resolving a tag is the same defect.
- `MatchAbandon`: the latch, plus the reason, the round it stopped on and the diagnostic.
  ⚠️ **It revokes DECIDING, not drawing.** Bodies keep interpolating; what stops is awarding.
  Two peers that both lost the host therefore stop at the state they last agreed on.
- `NetAuthority.ShouldResolve() => IsHost && !MatchAbandon.AuthorityRevoked`, asked in ONE place
  rather than at forty call sites, which is what that method exists for.
- ⚠️⚠️ **THE DISARM IS A SCENE SUBSCRIPTION AND NOT A CALL SITE.** A latch that outlived its match
  would take the SOLO game down with it, since `ShouldResolve()` is what runs single player, and
  `CLAUDE.md` § 4a is blunt about what happens to rules somebody has to remember. Leaving the arena
  is the signal, it cannot be forgotten, and `MatchRpc.HandleClientDisconnected` already causes it.
- The three readers of the raw disconnect string, the telemetry bucket, the player line and now
  the latch: all ask `SessionEndRules.Classify` instead of each deriving their own meaning
  (§ 94.1).
- **The player line names host loss** rather than "lost connection", because a player told only
  that they were disconnected goes looking for a fault in their own wifi.

`NationalsHardeningTests` asserts the revocation table, every reason string this game can actually
produce, the diagnostic naming the round, and that the latch clears without wiping the reason.

⚠️⚠️ **THE RECONNECT-OR-FORFEIT RULING IS STILL NOT TAKEN AND IS NOT CODE.** `Attention.md` § 17.1,
which also records that a refereed bracket may make the question nearly moot: on a
`-tp-dedicated` server no player leaving can end a match at all.

⚠️ **Host migration is deliberately unsupported and that is not the problem.** The requirement is
that the failure is ONE outcome on every peer: no score corruption, no frozen authoritative state
pretending the match continues, a stated reason, and a working way back.

⚠️⚠️ **THE RECONNECT AND FORFEIT RULING IS NOT A CODE DECISION AND IS NOT IN THIS QUEUE.** § 140.5
says a drop and a quit are the same event on the wire; whether a bracket match is replayed,
resumed or forfeited is `Attention.md`.

### 143.10 ⚠️ OPEN: `VISION.md` § 2 RULE 1 CONTRADICTS ITSELF

The arena is `CONFINEMENT_RADIUS` 7.0, so 14 m by 14 m = **196 m²**. Rule 1 reads *"about 1.8 to
2.5 m of radius, which is 3 to 8 per cent of the box"*. Both halves cannot be true:

| Radius | Area | Share of 196 m² |
|---|---|---|
| 1.8 m | 10.18 m² | **5.19%** |
| 2.5 m | 19.63 m² | **10.02%** |
| **1.37 m** | 5.88 m² | 3% |
| **2.23 m** | 15.68 m² | 8% |

⚠️ **Do not settle it by editing whichever half is easier.** The radii name Sean's Fire Trail and
Zack's Shock Trail as the reference *"and nobody has ever complained about them"*, so the RADII are
the observed thing and the percentages are the arithmetic that was never done. **Read the abilities
first**: rule 1's own text also says these two are measured as discs and played as corridors, which
is § 143.11.

### 143.11 ⚠️ OPEN: EVERY FOOTPRINT NUMBER PREDATES THE CURRENT ABILITIES

`Hero_Strike_Balance.md` and `VISION.md` § 2 carry a **81.9%** worst credible frame and a **27.2%**
Zack corridor. Both were measured before the ability retune that put Bolt Sprint on 46 s and Flame
Rush on 50 s, and before Thunderstrike became aimed. **They are history, not measurements of this
commit**, and nothing regenerates them.

### 143.12 ⚠️ OPEN: ABILITY COMMENTS DISAGREE WITH THEIR OWN CONSTRUCTORS

Confirmed by reading both on `e85b0fc`:

| Where | The comment says | The constructor passes |
|---|---|---|
| `ZackHeroKit.cs:86` vs `:102` | *"30 s, UP FROM 6.0"* | **46.0f** |
| `SeanHeroKit.cs:58` vs `:64` | *"34 s, UP FROM 6.5"* | **50.0f** |

⚠️ **And the second one repeats the first**: Sean's note reads *"Longer than Zack's 30"*, so one
stale number has already propagated into a second file as a comparison. That is the whole argument
for an audit rather than a correction.

✅ **CLOSED 2026-09-04. It was five, not two**, and `tools/audit_ability_stat_drift.py` is the gate:

| Where | Asserted | Ships | Also stale |
|---|---|---|---|
| `ZackHeroKit.cs:86` | 30 s | **46.0f** | "Three casts a round" (it is 1.96) |
| `SeanHeroKit.cs:58` | 34 s | **50.0f** | "Longer than Zack's 30"; "2.6 casts a round" (1.8) |
| `DanteHeroKit.cs:159` | 45 s | **62.0f** | "Two casts a round" (1.45) |
| `NemuHeroKit.cs:41` | 36 s | **52.0f** | "between SEAN'S 34 and DANTE'S 45"; "2.5 casts a round" (1.7) |
| `PhaisterHeroKit.cs:128` | 36.0 s | **52.0f** | the `<summary>` line |

⚠️⚠️ **THE AUDIT IS THREE NARROW PATTERNS AND NOT ONE WIDE ONE, AND THE REASON IS IN THE FILE.** The
wide rule ("a duration in a comment must exist as a literal in this file") found all five and three
false ones, **and every false one was a comment doing its job**: `HeroAbility.cs:115` quotes the
team asking for *"like 30 seconds to 45 seconds"*, which is the REQUEST that caused the retune;
`DanteHeroKit.cs:163` says *"At 9 s it was up for four seconds out of every nine"*, which is the
HISTORY `CLAUDE.md` § 3 asks for; `CheskaHeroKit.cs:182` argues against a rejected 3.2. **A rule
that cannot tell a stale fact from a recorded reason gets switched off.** False negatives are
accepted; false positives are not.

### 143.18 ⚠️⚠️ OPEN: ENTERING MATCH SETUP OVERWRITES THE SELECTED RULE SET WITH WHATEVER THE PLAYER LAST SAVED, AND THAT REACHES THE TOURNAMENT PRESET

**This is the root cause of six of the twelve failures in the isolated `screens` group, and it
started as a test mystery and ended as the exact hazard § 143.3 was built to prevent.**

`ConvertedMatchSetup`, on entry:

```csharp
if (!SceneFlow.Networked || NetAuthority.IsHost)
{
    SceneFlow.SetSelectedRules(CustomGameRules.Parse(
        Settings.SettingsStore.Current.CustomRulesWire, SceneFlow.SelectedMode));
}
```

⚠️ **The intent is right and is written up in its own note**: restore what this player left the
lobby on, and let a client adopt the host's set instead. **What it also does is silently discard a
rule set somebody set deliberately three lines earlier.**

**The measurement, from this machine's real `settings.json`:**

```
CustomRulesWire = 0|0|8|90|0|3|0|1|0
                  ^ ^
                  | format
                  mode 0 = Classic
```

**Mode 0 is Classic and Rounds is 8**, which is not a format the game ships at all: Classic plays
four rounds and Hero Strike eight (`docs/VISION.md` § 1.1). So the saved wire is itself carrying a
configuration nobody chose, and every entry into MATCH SETUP restores it.

**What that costs, in two places:**

1. ⚠️⚠️ **A TOURNAMENT MATCH.** `TournamentGuard.Apply()` sets Classic, four rounds, no bots,
   no score target. **Opening match setup then replaces all of it with the saved wire.** That is
   § 143.3's whole thesis (*"a mostly tournament match is the failure mode"*) arriving through a
   door nobody was watching, and the preset's own tests cannot see it because they never load the
   scene.
2. **The Hero picker builds for the wrong mode.** `ConvertedCharacterSelect.BuildStageDoors` reads
   `SceneFlow.SelectedMode == GameMode.HeroStrike` to decide whether to build the LOADOUT door, and
   the attribute strip is chosen the same way. With Classic restored underneath it:
   - `LoadoutSurfaceProbe`, **five cases**: *"no 'LoadoutDoor' on the character select stage"*.
   - `ModelPreviewTests.HeroCharacterSelectShowsAbilitiesInsteadOfClassicAttributes`: the HERO
     picker draws **`SPEED POWER GRIT`**, which is CLASSIC's attribute strip, instead of naming
     Dante's skills. `docs/VISION.md` § 3 is the rule that breaks: *"a player must be able to get
     what all skills do just by looking at them, or reading them from char select."*

⚠️⚠️ **AND THE PROBES WERE RIGHT ALL ALONG, WHICH IS THE PART WORTH SITTING WITH.** Both set
`SceneFlow.SelectedMode = GameMode.HeroStrike` and then load `MatchSetup`, which is exactly what
the game does, and the scene overrode them. **In the single-process full run this was invisible**:
all five `LoadoutSurfaceProbe` cases died earlier, at *"MatchSetup has no CharacterSelectPanel to
open"*, which is a different claim about a different object and is not true in isolation. **A
phantom failure was sitting on top of a real one for four full runs.**

**Done looks like** a rule set that was set deliberately surviving the trip into match setup, with
the restore still happening for an ordinary player who did not set one. ⚠️ **Do not fix it by
deleting the restore**: its note is correct, and a player who leaves the lobby on a custom set and
comes back to the shipped one is a regression somebody will report.

### 143.19 ✅ CLOSED 2026-09-04: THE HERO PICKER SPENT ITS ONE SENTENCE ON THE WRONG STRING

**Found underneath § 143.18**: once the mode stopped being overwritten, the picker built for Hero
Strike and the assertion moved on to a second, real fault.

`ConvertedCharacterSelect` picks the ability blurb like this:

```csharp
if (slotView.Total >= 2 && slotView.Equipped != null)
    summary = slotView.Equipped.Description;
```

and the note above it says *"THE VARIANT'S OWN DESCRIPTION WINS OVER `HeroAbility.Summary`, and
the two are the same string on a default build, so a fresh account sees no change at all."*

⚠️⚠️ **THAT STOPPED BEING TRUE AND NOTHING NOTICED, WHICH IS § 143.12'S DRIFT ONE LEVEL UP: A
COMMENT ASSERTING TWO STRINGS ARE EQUAL WHEN THEY ARE NOT.**

| | |
|---|---|
| `DanteHeroKit.cs:49` summary | *"Ground slam. Shoves players and tsinelas away from you."* |
| `HeroLoadout.cs:176` default variant description | *"A 2.2 m shock at your feet that launches whoever is standing in it."* |

**Two strings for one default ability, in two files.** So every fresh account was reading the
variant's long description on the one screen whose whole job is the short one.

⚠️ **`docs/VISION.md` § 3 is the rule it breaks.** The LEARN layer (character select) carries
*"icon, name, what KIND of power it is, ONE SENTENCE, cooldown"*; the full text belongs to the
RECALL layer behind the hold key. Spending character select's one sentence on the recall string is
the three-layer answer collapsing into two.

✅ The variant's description now wins only when the equipped variant is **not** the default one,
which is what the note always intended. ⚠️ **The two strings are still different and that is left
alone deliberately**: making them equal would be a content decision about how Seismic Stomp is
described, and the screens now each show the one they were designed for.

### 143.17 ⚠️ OPEN: TWO DEFECTS THAT WERE INVISIBLE UNTIL THE SUITE WAS ISOLATED

**Both were sitting underneath a phantom failure and neither could be seen while the full run was
blaming them for somebody else's leak.**

- ⚠️ **`LoadoutSurfaceProbe`, five cases: *"no 'LoadoutDoor' on the character select stage. In Hero
  Strike `BuildStageDoors` builds LOADOUT above MAKE YOUR OWN"*.** In the full run all five said
  *"MatchSetup has no CharacterSelectPanel to open"* instead, which is a different claim about a
  different object and is not true in isolation.
- ⚠️ **`ModelPreviewTests.HeroCharacterSelectShowsAbilitiesInsteadOfClassicAttributes`: *"Dante's
  first skill is not named on the Hero picker"*, and the screen reads `SPEED POWER GRIT`**, which
  is the CLASSIC attribute strip on the HERO picker. `docs/VISION.md` § 3 is the rule it breaks:
  a player must be able to learn what a power does from character select.

⚠️⚠️ **THIS IS THE ARGUMENT FOR § 143.1 STATED AS A COST RATHER THAN AS A PRINCIPLE.** A gate that
reports thirty failures, of which twenty-five are phantoms, does not merely waste time: **it hides
the five.** Nobody reading *"MatchSetup has no CharacterSelectPanel"* for the fourth run running
goes looking for a missing door.

### 143.13 to 143.16

Storage failure hardening, the gameplay clock audit, the cold-start test and the crash bundle. Each
is stated with its acceptance criteria in the CURRENT IMPLEMENTATION QUEUE at the top of this file.

---

## 141 · SPECTATOR AND A DRIVEN SEAT WERE ON SCREEN AT THE SAME TIME, AND F1-F4 HAVE TWO READERS ⚠️⚠️ OPEN, 2026-09-04, branch `abilities-rework`

🧑 2026-09-04, with a screenshot: *"dude is this shti spectator?"*, *"if this is spectator why tf
can i move the character by doing wasd"*, **"IF IT isnt spectator why do i see spectator hud"**.

**He is reading it correctly. Both halves are genuinely live at once**, which is the same sentence
§ 130's predecessor recorded in his own words on 2026-08-27: *"for some reason im in spectator but
im also defender"*, *"its so weird bcz spectator has skills and defender UI"*. That one was fixed
by making the HUD follow the camera rather than the switch (`MatchInstaller` around line 1290).
**This is a different route into the same broken state.**

### 141.1 WHAT THE SCREENSHOT SHOWS

| On screen | Which system owns it |
|---|---|
| A first-person hand holding a tsinelas | `CameraSystem.ViewmodelArms`, on the **gameplay rig** |
| WASD moves a character | `PlayerInputReader` on a **driven seat** |
| `FREE FLIGHT · 3.6 m/s` | `SpectatorCamera.StatusText`, so the spectator camera is **in free mode** |
| `SPECTATOR  F1-F4 player POV · TAB follow · V POV/chase ...` | `SpectatorCamera.ControlsText` via `Hud.BuildSpectatorReadout` |
| `[C] CONTROLS OVERLAY` | `Hud._spectatorHint` |
| **`PLAYER#7645` twice in the scoreboard**, once DEFENDER 280 and once ATTACKER 0 | two seats carrying the local player's name |

⚠️ **THE DUPLICATE NAME IS THE PART THAT SAYS "A SEAT WAS SEIZED".** One human name on two rows is
what a seat handover looks like when the old row was never given back.

### 141.2 ✅ VERIFIED: F1, F2, F3 AND F4 EACH HAVE TWO READERS

Read from the source on 2026-09-04:

| Reader | What it does |
|---|---|
| `DebugPlayerSwitcher.Update:114-117` | `Assign(0)` / `Assign(1)` / `Assign(2)` / `Assign(3)`: **seizes that seat**, un-parks it and gives it the input reader |
| `SpectatorCamera.Update:782-785` | `SelectPlayerPov(0..3)`: puts the spectator camera in that seat's POV |

**One press does both.** That is `CLAUDE.md` § 4's *"one control, one action, per context"* broken,
and it is the SAME hole § 136.1 found six hours earlier on F1 and did not finish closing.

⚠️⚠️ **§ 136.1 LOOKED STRAIGHT AT THIS AND MISSED IT, AND THE REASON IS WORTH KEEPING.** It found
three readers of F1, moved the sandbox toggle to F7, and justified the choice with *"F7 is the
first key past the block `DebugPlayerSwitcher` owns (F1-F4 seats, F5 cycle, F6 default)"*. **It
treated F1-F4 as belonging to the switcher and never asked whether anything else was also reading
them.** It was fixing the odd one out rather than auditing the block, and `SpectatorCamera` was
the reader it walked past.

⚠️⚠️ **AND THIS BREAKS THE PREMISE § 35.3'S EXEMPTION RESTS ON.** `CLAUDE.md` § 4 allows the
spectator set to reuse gameplay keys for one stated reason: *"a spectator has no body, no seat and
no `CharacterMotor`, so while watching every gameplay action is inert and while playing none of the
spectator set is reachable: they can never both fire."* **`DebugPlayerSwitcher` can hand a
spectator a body**, so there is a state in which both ARE reachable, and the exemption's own
sentence stops being true. That is a bigger finding than the key clash: **the rule that makes
nine spectator keys legal has a counter-example.**

### 141.3 ✅ THE CAUSE, AND IT IS NOT THE KEY COLLISION

⚠️⚠️ **`Hud.EnterSpectatorMode` HAD NO INVERSE, AND `MatchInstaller.RebindLocalSeat` IS
HALF-WRITTEN BECAUSE OF IT.** Its non-watching branch re-enables the gameplay rig, re-follows the
seat, and puts the watcher camera away, under a note that says exactly what it is doing:

> ⚠️ A REBIND CAN ALSO ARRIVE AFTER A SPECTATOR WINDOW, so the watcher camera is put away rather
> than left running beside the rig.

**It puts the CAMERA away and nothing puts the SCREEN back**, because there was nothing to call.
So every re-seat after a spectator window leaves the HUD stripped and the controls overlay drawn
over a body the player is driving. That is the screenshot, and the first-person arms that made the
first reading fail are simply the rig being correctly re-enabled on the line above.

⚠️⚠️ **AND THE READOUT SURVIVES A `_spectating` FLAG ALONE, WHICH IS WHY CLEARING ONE WOULD NOT
HAVE BEEN ENOUGH.** `_spectating` gates whether `UpdateSpectatorReadout` RUNS. It does not hide
anything: `BuildSpectatorReadout` creates four `Text` objects, sets `_spectatorHint.enabled = true`
once, and writes the legend's key list at build time and never again. **They keep drawing whatever
they last said for the rest of the match.**

✅ **Fixed**, and in the three places that can seat somebody:

| | |
|---|---|
| `Hud.ExitSpectatorMode` | New. The mirror of `EnterSpectatorMode` in the same order: gives back the caster cell's width, disables the four readout labels, and re-activates the stacks, the indicators and the YOU card. ⚠️ **It does NOT re-show `RoleSwapCard`**, which is a transient that shows itself; forcing it would put a stale swap announcement on screen at the moment somebody is handed a seat |
| `MatchInstaller.RebindLocalSeat` | Calls it **before** `Bind`, so no frame is spent drawing a seated HUD that still believes it is watching |
| `MatchHost.ExitSpectatorMode` | Calls it too. Re-activating the HUD GameObject is not undoing a strip that was applied by a different component |
| `DebugPlayerSwitcher.ApplySlots` | Disables the spectator camera and calls it, because a body driven by a keyboard is not a body being watched |

⚠️⚠️ **AND `ApplySlots` HAD A SECOND BUG THE SAME LINE OF READING FOUND: IT LOOKED THE RIG UP
THROUGH `Camera.main`.** `MatchInstaller` tags the `SpectatorCamera` it builds as `MainCamera`, so
on any run that started with nobody driving a seat, `Camera.main.GetComponent<CameraRig>()`
answered **null** and the whole camera handover silently did nothing: the seat changed hands and
the view stayed where it was. That is the exact symptom the note above it says the block exists to
prevent, *"indistinguishable from Tab doing nothing"*. It is found by type now, and switched back
on, because the same branch that built the spectator camera had disabled it.

### 141.4 ✅ THE KEY COLLISION, AND WHAT IS STILL OPEN

✅ **`SpectatorCamera` no longer reads F1-F4 when a `DebugPlayerSwitcher` is present offline.** The
switcher has the older claim, is what `DebugBar.KeysText` advertises, and is the one that seizes a
seat. ⚠️ **Networked is untouched and is the other way round**: the switcher returns on its first
line in a networked session, so there is no second reader and a real spectator keeps its POV keys.
**The gate is who else is listening, not which mode we are in.**

### 141.5 ✅ THE `#7645` IS OFF THE IN-MATCH LABEL

🧑, with a crop of the four rows: *"no need to show # number in the thing gang"*, *"just the player
name is enough here"*.

`MatchInstaller` seats a human with `GameServices.Account.LobbyName`, which is
`AccountRules.Handle(DisplayName, Discriminator)`, so the tag rode into the arena on the
scoreboard, the nameplate over each body, the YOU card and the spectator's follow line.

✅ **Stripped in `CharacterMotor.DisplayName` and nowhere else**, through the `TrySplitHandle` that
already existed. ⚠️ **AT THE LABEL, NEVER AT THE SOURCE**: the discriminator is what makes two
players called PLAYER different people, and `LobbySession`, the lobby list and the wire all need
it. `DisplayName` is the in-match label and nothing else reads it, so this is exactly as wide as
the request. It falls back to the whole string, because a name that never went through `Handle` has
no tag to take off.

⚠️ **AND `MatchStatsCollector` KEEPS THE FULL HANDLE**, which is the half that would have been lost
silently. A stored match row outlives the match and a history listing strangers has nothing else to
tell two players called PLAYER apart; the local seat was already overwritten from the account, and
this keeps a REMOTE seat's row as informative as it was.

### 141.7 ✅ 2026-09-05: THE INVARIANT MODEL CAN NOW EXPRESS BOTH FAULTS, WHICH IT COULD NOT

`MatchInvariants.CheckSeatOwnership`'s own note claimed both directions:

> *"Two seats with one owner is the spectator-and-a-driven-seat fault (§ 141) ... One seat with two
> owners is the reconnect fault: a peer whose slot was reused comes back and both believe they are
> driving it."*

⚠️⚠️ **THE FIRST IS FINDABLE IN AN OWNER-PER-SEAT ARRAY. THE SECOND IS NOT REPRESENTABLE IN ONE.**
`MatchSnapshot.SeatOwners` is a `string[]` indexed by slot, so "two peers claim seat 2" is not a
state it can be IN: whichever wrote the cell last is the only one a checker will ever see, and the
question was answered by the data structure before the checker was asked. **A comment claiming an
invariant the data cannot express is worse than no comment**, because it is read as coverage.

**So a claim is a ROW now, not a cell.** `SeatClaim` carries an owner token, a seat, whether it is
DRIVING and whether it is SPECTATING, and `CheckSeatClaims` finds three faults, each with a receipt
in this repository:

1. **One owner driving two seats**, § 141, the duplicate name 🧑 photographed.
2. **Two owners driving one seat**, the reconnect window. `NetSession.OnClientConnected`
   disconnects the stale socket *after* the new one takes the chair and warns that otherwise *"it
   can keep submitting movement and verbs for the same player"*. That window is exactly this state
   and nothing could ask about it.
3. **A spectator driving a seat**, § 141's headline: **"IF IT isnt spectator why do i see
   spectator hud"**.

⚠️ **A HELD CHAIR IS A CLAIM THAT IS NOT A DRIVER**, and only drivers are counted for rule 2. The
reconnect feature deliberately produces a seat whose claimant is driving nothing, and reporting it
would be reporting the feature every time it worked.

⚠️⚠️ **AND `CheckPeersAgree` NOW COMPARES OWNERSHIP, WHICH ITS OWN HEADER SAID IT SHOULD** (*"§ 141
is the seat"*) and which it did not do. It compared the round, the taya, the progress flag, the
scores and the winner, **every one of which can be identical on two peers that disagree about who
seat 2 is**, and the seat decides whose tsinelas is whose, who a tag lands on and which line a
point is written to. ⚠️ An empty chair on one side only is not a disagreement: a client is
routinely mid-build (§ 82.1) and a bot seat has no owner token.

⚠️⚠️ **AND THE PRODUCER MADE THE CHECK UNFALSIFIABLE.** `FailureBundle` built its owner array as
`owners[slot] = "seat" + slot`, so every entry was distinct BY CONSTRUCTION and the duplicate rule
could not fire in any state the game could reach, § 96's fault one layer down, a green light wired
to nothing. `Diagnostics.SeatOwnership.Claims()` reads the real durable tokens off the lobby and
the bodies.

**Still open:** 🧑's eye on the fix.

### 141.8 ✅ 2026-09-05: THE DUPLICATE NAME, AND WHY IT IS NOT ONE FAULT

Two boards draw the four seats, `Hud`'s live one and `MatchResult`'s end-of-match one, and both
resolved a seat through **their own copy** of the same four-line helper:

```csharp
var who = GameServices.Round?.PlayerAt(slot);
return who != null ? who.DisplayName() : $"P{slot + 1}";
```

`CharacterMotor.DisplayName` answers what that BODY is called. It cannot know another seat answers
the same, and it is not its job to.

⚠️⚠️ **TWO IDENTICAL ROWS ARE TWO DIFFERENT FAULTS WEARING ONE APPEARANCE, AND THE FIX KEEPS THEM
APART BECAUSE THE BRIEF FOR THIS REFUSES TO HIDE EITHER:**

1. **Two different people sharing a name.** Every guest account arrives under the same handle until
   somebody types one, so four rows reading BATA is an ordinary Saturday. **Nothing is wrong with
   the game and everything is wrong with the board**: a player cannot tell which row is theirs.
   `SeatLabel.ForBoard` names the SEAT as well when a name collides — on **both** rows, because
   marking only the later duplicate makes the first look like the real one and there is no such
   thing.
2. **One person genuinely driving two seats**, which is § 141's real fault and a state the game
   must not be in. **`MatchInvariants.CheckSeatClaims` is what reports that** (§ 141.7 rebuilt the
   model so it could), and disambiguating the text would bury it.

So the invariant and the presentation agree without either doing the other's job: the board always
draws four distinguishable rows, and the checker is still the thing that says whether two of them
share a PERSON. A reader who sees `BATA · P2` and `BATA · P3` learns that two seats share a name;
a failure bundle is what says whether they share an owner.

⚠️ **ONE RULE, TWO CALLERS.** § 94.1 records four hand-written copies of *"which line in a record
is mine"*, all agreeing on the wrong value, as the reason nothing on the machine could see the
fault. `Hud.SeatName` and `MatchResult.NameFor` were two copies of this one, and
`BothBoardsAskTheOneNamingRule` fails if a third appears.

⚠️ **THE TAYA SENTENCE KEEPS THE PLAIN NAME.** `SeatLabel.Raw` is what `Hud`'s round line asks:
a row in a list has to be distinguishable and a sentence does not, and *"BATA · P2 IS THE TAYA"*
reads as a machine talking.

### 141.6 ✅ AND NO TWO SEATS CAN READ THE SAME, WHICH IS THE TOURNAMENT QUESTION

🧑, immediately after: *"oh yea in tournmanets PPL might have same #?"*, *"idk how it was coded but
make it so that they all have a diff #"*, and then the case that matters: **"offline
tournaments"**.

⚠️⚠️ **HE IS RIGHT, AND IT IS WORSE THAN THE QUESTION ASSUMES, IN TWO INDEPENDENT WAYS.**

**One: the tag guarantees nothing and cannot.** `AccountRules.Discriminator` is FNV-1a over the
stable player id, `% 10000`, computed **on each machine from that machine's own id**. Nothing
checks it against the other people in the room because nothing can. Two accounts sharing a display
name collide about one time in ten thousand per pair, which sounds safe until a bracket defaults to
the same name: `Handle` falls back to the literal `"Player"` for anything that fails
`TryDisplayName`, so a room of people who never set a name are all PLAYER drawing from 10,000 tags.
**Thirty-two of them collide about five per cent of the time.**

**Two, and this is the one that was already on screen: two seats can share a label with no accounts
involved at all.** `Logs/shots-runtime/Eskinita.png` read **ZACK, PLAYER, ZACK, PHAISTER**: two
bots picked the same character, so two rows carried the same name and the board could not say which
one was the taya. That needs no tournament to reproduce and is the same defect as the duplicate he
photographed.

✅ **`CharacterMotor.ResolveDuplicateLabels`, run from `Hud.UpdateScores`**, which is the one place
that sees all four seats at once. A per-machine hash cannot be made globally unique by better
hashing; four seats compared against each other can be made unique by construction:

1. **Labels that are already unique are left completely alone**, so *"just the player name is
   enough here"* stays true for every ordinary match.
2. A colliding pair gets its `#tag` back, which is the real distinction between two accounts and
   the thing he expected to differ.
3. If the tags collide too, or there are none, **the seat number is appended, and that cannot
   collide**: there are four seats and they are numbered.

**Measured on the next render:** `ZACK P1 · PLAYER · ZACK P3 · PHAISTER`, and the round line under
the clock follows it (`DEFENDER: ZACK P1`). The two unique names carry no suffix.

⚠️ **IT RUNS PER FRAME RATHER THAN AT SEATING, AND THAT IS NOT LAZINESS.** A joining peer's name
lands through `MatchRpc` after the arena is built and a seat can change hands mid-match, so
resolving once at install would be right until the first of those. It is sixteen string compares,
it runs before the score stamp so a duplicate cannot show for one frame and then correct itself,
and it writes nothing when the labels are already unique, which leaves `DisplayName`'s cache
untouched on the common path.

⚠️ **STILL NOT ESTABLISHED: why TWO SEATS carried the local player's name** in his screenshot.
`ApplySlots` sets `IsBot = !driven` and `DisplayName` branches on exactly that, so a vacated seat
should fall back to its character name. **The label is now always distinguishable either way**, so
this is no longer something a player can be confused by, but the underlying seat bookkeeping was
not proven correct.

⚠️⚠️ **AND THE `DebugKeys` CATALOGUE IS NOW OVERDUE.** § 136.1 called it *"the real fix"* and left
it undone; this entry is the second collision inside that same block in one day, found by a person
rather than by a test, because every one of these keys is a literal `Keyboard.current` read outside
the input map where `InputMapAndAbilityTests` cannot see it. **Until it exists, grep
`Keyboard.current` before binding any literal key** — that grep is exactly what would have caught
this one.

---

## 140 · THE PLAYER CANNOT SEE THE NETWORK, AND THE TIMEOUT GIVES THEM EIGHT BLIND SECONDS ⚠️⚠️ OPEN, 2026-09-04, branch `abilities-rework`

🧑 2026-09-04: *"thoroughly check if u broke network or if theres shit we can imrpvoe in network
and crossplay"*. Nothing was broken (§ 140.1). **This is the biggest thing that can be improved,
and it was found by measuring rather than by reading.**

### 140.1 FIRST, THE CHECK: NOTHING WAS BROKEN

The settings and touch work touched no file under `Runtime/Net/`. Re-run after it, on the same
commit as the tab overhaul:

| | Reading |
|---|---|
| `Core.Tests` | 489 passed, 0 failed |
| `audit_ability_authority.py` | 49 sites, 30 gated, **0 ungated on another body** |
| `audit_request_call_sites.py` | **59 wire entry points, 0 unreachable** |
| `audit_wire_payloads.py` | **61 named messages, 0 mismatched** |
| `audit_audio_reach.py` | 42 sites, **0 host-only** |
| `audit_presentation_reach.py` | 96 sites, 96 reachable, **0 host-only** |
| `audit_cue_relay.py` | 48 sites, **0 UNGATED** |
| `NetSession.ProtocolVersion` | **23, unmoved** |

### 140.2 ⚠️⚠️ THE GAP: THERE IS NO CONNECTION READOUT OF ANY KIND

Grepping the whole runtime for `Rtt`, `ping`, `latency`, `Unstable` and `reconnect` on 2026-09-04
returns **one comment about audio latency and nothing else.** No ping number, no bars, no
"reconnecting", no "connection lost" on any screen in the game.

⚠️⚠️ **AND `ConfigureTimeouts` TURNS THAT INTO AN EIGHT SECOND HOLE.** `DisconnectTimeoutMS` is
**8000**, chosen deliberately and reasoned in its own note: *"eight seconds of complete silence on
either is a machine that has gone, not a machine that is late."* § 137.5 measured both sides of
that number for the first time:

| Outage | What happened |
|---|---|
| 5 s | **Survived.** No disconnect logged at all; both peers still agreed on every discrete field |
| permanent | Dropped at the timer, client fell back to its own lobby |

**So a peer whose wifi dies keeps a completely normal-looking arena in front of it for eight
seconds.** Bodies keep interpolating, the HUD keeps drawing, the clock keeps running, and nothing
anywhere says the machine has stopped hearing from the referee. For a brief that is explicitly
*"a tournament room, bad wifi, no second chance"*, those eight seconds are the emptiest part of
the whole stack, and they are exactly the ones somebody standing behind the player needs to read.

### 140.3 ✅ THE MEASURABLE HALF IS BUILT

`NetSession.SampleLink` samples `UnityTransport.GetCurrentRtt` once a second on **both** peers and
publishes `NetSession.LinkRttMs` and `NetSession.Link` (`Unknown` / `Good` / `Poor` / `Bad`).

- ⚠️ **ON THE CLIENT TOO, AND THAT IS THE POINT.** `NetSession.Update` returns early unless
  `IsServer` for everything else it does; the client is the peer that suffers a bad link and the
  one whose player needs telling, so sampling only on the host would measure the machine least
  likely to be the problem.
- ⚠️ **THE HOST REPORTS ITS WORST PEER, NOT AN AVERAGE.** A host with three good clients and one
  on a dying phone has a problem, and a mean of four hides the one peer anybody cares about.
- ⚠️ **THE BOUNDS ARE § 137.5'S OWN ROWS RATHER THAN PICKED NUMBERS.** Poor at 200 ms, because 150
  measured *"indistinguishable from the clean row"*; Bad at 450, because 600 measured *"degraded
  but connected"* and is where `PlausibleIntentPose` is expected to start refusing verbs (§ 135.3).
- **It logs on a state CHANGE, never on a sample**, which is what `tools/net_matrix.py` can read
  off `Player.log` and therefore what can be asserted. A line per second per peer would be noise
  in exactly the log somebody is reading to find out what went wrong.
- ⚠️ **NO PEERS IS `Unknown`, NOT `Bad`.** A host alone in a lobby has nothing to measure, and
  reporting that as a bad link would light a warning on the one screen where nothing is wrong.

### 140.4 ⚠️ WHAT IS NOT BUILT: THE SCREEN

**Deliberately not built, because a new always-on HUD element is a design pass and not a field.**
`CLAUDE.md` § 4a rule 4 asks three questions of anything that is not a screen or a verb, and § 6.2
asks what the ONE thing on the screen is. An indicator that is on all the time is a permanent tax
on the readability budget `docs/VISION.md` § 2 sets in a 14 by 14 metre box.

**The design, so the next session does not start from a blank page:**

1. ⚠️⚠️ **IT APPEARS ONLY WHEN IT IS NOT `Good`.** A ping number in the corner of every match is a
   number the player has to learn to ignore. The whole value is in the transition, so the control
   that is invisible at `Good` is the one that means something when it is not.
2. **Three states, not a number.** `Poor` and `Bad` are the two the player can act on (wait, or
   tell the room); the millisecond count belongs in the log, not on the arena. `docs/VISION.md`
   § 3's rule about the HUD carrying no sentences applies.
3. ⚠️ **AND THE COUNTDOWN IS THE PART THAT IS ACTUALLY NEW.** Above about four seconds of silence
   the honest thing to draw is how long is left of the eight, because that is a fact the player
   can do something with and it is the one thing no other game element can imply.
4. **On the HUD canvas**, which means top centre or top right, in the carved wood palette
   (§ 133.4 keeps the HUD in the old colours on purpose). ⚠️ **Not `TouchHud`'s canvas**: this one
   is a readout rather than a control, so it does not need a raycaster and must not steal a press.
5. **Three devices**: it is not interactive, so the pad and thumb answers are both "nothing to
   press". That is a written answer rather than silence, which is what § 4a asks for.
6. **The host also needs the per-peer version**, because the host is the one who has to decide
   whether to wait. A row on the pause menu naming which seat is struggling is the cheap shape.

### 140.5 ⚠️⚠️ AND THE ONE NOBODY CAN FIX WITHOUT DECIDING SOMETHING: A DROP AND A QUIT ARE THE SAME EVENT

§ 137.6's third row measured this on purpose. **"The peer went away" and "the network went away"
are the same event to the transport**, both end at the same 8 second timer, and both produce the
same bot in the same seat. In a tournament room those are two different sentences to say out loud,
and the game currently cannot say either.

⚠️ **THIS IS A PRODUCT DECISION BEFORE IT IS AN ENGINEERING ONE.** An orderly quit already sends a
real `DisconnectClient` (`ConfigureTimeouts`'s note says so), so the two ARE distinguishable at the
transport: a clean disconnect versus a timeout. **Nothing reads that difference and nothing shows
it.** Answering it is worth more to a bracket than any latency work, because it decides whether a
round is replayed.

---

## 139 · SETTINGS IS FOUR PAGES NOW, AND THE RENDERS FOUND THREE FAULTS THAT HAD SHIPPED FOR THE WHOLE PORT ⚠️ OPEN, 2026-09-04, branch `abilities-rework`

🧑 2026-09-04, three asks in one message: *"make controller settings in settings look prettier
too"*, *"make it rebindable in controller"*, and *"can u overhaul how settings is organized? maybe
add tabs or some shit so that they dont have to scroll that much"*, *"we have too many settings
now"*.

### 139.1 THE BASELINE, AND ALL THREE COMPLAINTS ARE VISIBLE IN ONE PICTURE

`Logs/shots-runtime/Settings-v91.png`, taken 2026-09-03, before any of this. Everything in the
panel was in ONE `ScrollRect`: the visible page was the username, the device pair and six MOVEMENT
rows, and the scrollbar handle was about a sixth of its track. Below the fold, unseen, were the
mouse sensitivity, invert Y, fullscreen, render style, vertical sync, anti-aliasing, the slipper
highlight, three volume sliders, the telemetry picker and its privacy disclosure.

⚠️ **SO "TOO MANY SETTINGS" IS EXACTLY RIGHT AND IT IS NOT A COUNT PROBLEM.** `CLAUDE.md` § 6.2's
third claim names this failure mode: *"everything the feature can do is on screen at once, in one
flat list, with nothing saying what matters."* A player looking for the music volume had no way to
know it existed.

### 139.2 ⚠️⚠️ "MAKE IT REBINDABLE IN CONTROLLER" WAS ALREADY BUILT, AND SAYING SO MATTERS

**Per-device rebinding shipped on 2026-09-02** (§ 125.13, § 126.6). `Rebinding.ResolveBindingIndices`
returns every binding for an action, `TryRebind` writes the override onto the binding for the device
the player just pressed, `BeginRebind` restricts the candidate paths to the page's own device, and
the pad page cancels on the pad's own B. **None of that needed building.**

⚠️ **WHAT WAS WRONG WAS THAT NOBODY COULD FIND IT, WHICH IS § 96 AGAIN.** The KEYBOARD / GAMEPAD
pair was a row inside the scroll, behind a `MenuBody` label reading **"Showing"**, drawn at
`BindingControlSize` (170 by 46), which is the exact size and shape of the keycaps under it. So the
one control that reinterprets every row on the page was drawn as a settings row with a value, in
the 260-unit column that means "the name of a control you can rebind", under a word that is not a
noun anybody would search for. **A feature nobody can find is not a feature**, and the fix is
presentation rather than plumbing.

### 139.3 WHAT LANDED

- **Four section tabs** above the scroll and outside it: CONTROLS, AUDIO, VIDEO, PLAYER.
  ⚠️ **OUTSIDE THE SCROLL IS THE POINT**: a strip that scrolls away with the rows it selects is a
  control the player loses the moment they use the list.
- **The three headings the tabs made redundant are hidden**, not deleted: `BindingsHeading`,
  `AudioHeading` and `DisplayHeading` each repeated the tab above them. `MouseHeading` stays,
  because that one is a sub-section inside CONTROLS rather than a name for the page.
- **The device pair is a full-width rail of two** directly under the section strip, at 52 units
  against the strip's 56 and a row's 46. ⚠️ **THE HEIGHT IS THE HIERARCHY SAID OUT LOUD**: the
  strip switches the section, this pair switches every row in one section, a row is a row. The
  word "Showing" is gone rather than reworded.
- **`HintLabel` is per tab and follows the device page.** It read *"Click a key to rebind it, then
  press any key on your keyboard"* on a panel that also carries a gamepad page, three volume
  sliders and a privacy disclosure, so it was telling a pad player to press a key.
  `docs/VISION.md` § 3: *a screen that teaches the wrong key is worse than one that teaches none.*
- **The username moved out of `BindingsList`.** It was a child of the rebind list under a heading
  saying CONTROLS, and `BuildRebindRows` carried a `if (child.name == "PlayerNameRow") continue;`
  to avoid destroying it while clearing the list, which is the shape of a row in the wrong parent.
- **The scrollbar hides on a page with nothing to scroll**, and only the bar: the wheel and the
  drag stay live so the behaviour does not change under the player between pages.

### 139.4 ⚠️⚠️ THREE FAULTS THE RENDERS FOUND, ALL THREE OLDER THAN THIS PASS, ALL THREE INVISIBLE UNTIL NOW

**This is `CLAUDE.md` § 6.2b working exactly as written, and the mechanism is worth stating: the
tabs did not cause any of these, they PHOTOGRAPHED them.** Every one of these rows was below the
fold of a single scroll that opened on MOVEMENT, so no render of this screen ever taken, `v57`
through `v91`, had reached them.

| What | Where | The rule it broke |
|---|---|---|
| ⚠️⚠️ **The three volume slider fills were BRIGHT BLUE** | `MasterVolumeSlider`, `SfxVolumeSlider`, `MusicVolumeSlider`, authored in the `.tscn` | `CLAUDE.md` § 6.4, stated as wide as it goes: *"no blue, no navy, no cold grey, in any UI colour, in any layer ... fills"*. 🧑 has said this six times. **`AUDIO` is now a page you land on and they were the first thing on it** |
| ⚠️⚠️ **The FULLSCREEN tick was MAGENTA** (`UiTheme.Impact`, `f468a8`) | `FullscreenCheck` | `Impact` is a HUD colour for a gameplay event. § 6.4's palette is the eight measured logo colours and magenta is not one of them |
| ⚠️ **The telemetry note drew ABOVE the row it explains** | `BuildTelemetryNote` | Its own comment claimed the opposite and contradicted itself in two sentences. Every row inserts at `FullscreenCheck` + 1, so last in is highest on screen, so inserting the note after the row put the explanation above the control |

✅ **Both colours are `UiTheme.BrandPersimmon` now, and that is a ROLE rather than a pick.** § 6.4's
table gives `#FD8041` exactly one job: *"the MARKER: the one value or selection that matters"*. A
slider fill and a ticked box are the same statement, so they are the same colour. The slider track
is `PaperSunk` and the handle is `Paper`, which are the paper ramp's own two surfaces.

### 139.5 ⚠️⚠️ AND ONE FAULT THE TEST FOUND, WHICH IS THE HALF A RENDER COULD NOT HAVE

The first version of the strip looked up its rows with `ConvertedScreen.Node`. **That reads
`_byName`, an index built by walking the tree ONCE before `Wire` runs**, and seven of the rows the
strip owns do not exist at that moment: `RenderStyleRow`, `AntiAliasRow`, `VSyncRow`,
`SlipperHighlightRow`, `TelemetryRow` and `TelemetryNote` are created DURING `Wire`, and
`PlayerNameRow` is reparented during it.

**So six rows would have been visible on all four tabs at once**, while `Node` logged an error for
each one. `TabNode` scans the scroll content's own children instead.

⚠️ **`MissingTabNodes` AND ITS ASSERTION IN `TheSettingsPanelDraws` ARE THE PART THAT STAYS.** A
stale tab node name is otherwise silent: `ShowTab` skips a null, so the row is simply never shown
on any page and nothing says so. The check needs a BUILT panel, which is why it lives in the shot
test rather than in EditMode.

### 139.6 THE RENDERS

⚠️ **FOUR FILES, ONE PER TAB, AND THAT IS NOW THE RULE FOR THIS SCREEN.** `CLAUDE.md` § 6.2b:
*"EVERY STATE, not the one you built first. A screen with a mode has two layouts and you have
looked at one."* This screen has four, so a single `Settings-vN.png` is a picture of a quarter of
it. `TheSettingsPanelDraws` drives `ShowTab` and captures each, with the page name in the filename
so nothing is overwritten.

`Logs/shots-runtime/Settings-v93-CONTROLS.png`, `-AUDIO.png`, `-VIDEO.png`, `-PLAYER.png`.

### 139.7 ⚠️ WHAT IS STILL OPEN

- ⚠️⚠️ **🧑 HAS NOT LOOKED AT IT YET, AND THAT IS THE ACCEPTANCE TEST.** `CLAUDE.md` § 6.2 is his
  three claims and none of the three is visible to any probe in this repository. **This entry
  stays OPEN until he has opened a build and said the pages are right.**
- **The four groupings are a judgement and the fourth is the softest.** PLAYER holds the username,
  the slipper highlight and telemetry on the argument that all three are about YOU rather than
  about the game's picture or its sound. If any of them reads as misfiled, moving it is one line
  in `Tabs`.
- ⚠️ **A SHORT PAGE IS MOSTLY EMPTY, AND NOTHING WAS DONE ABOUT IT.** The panel is a fixed height
  because CONTROLS needs it, so AUDIO's three rows sit above a lot of paper. The honest options are
  a shorter card on short pages, or accepting it; **it was not measured against his window**, which
  is § 6.2b's third row and the one this repository gets wrong most often.
- **The blue and magenta were found by looking, not by grepping**, which § 6.4 explicitly says is
  the wrong way round: *"CHECK IT BY GREPPING, NOT BY LOOKING."* That rule names `UiTheme.cs`, and
  both of these were authored in a `.tscn` and converted, so the grep it describes would not have
  found either. **Nothing yet greps the converted prefabs for a cold fill**, and that is a real gap
  now that two have been found this way.

---

## 138 · A CONTROLLER UNITY DOES NOT RECOGNISE IS INVISIBLE TO THIS WHOLE GAME ⚠️ OPEN, 2026-09-04, branch `abilities-rework`

⚠️⚠️ **STEPS 1, 2 AND 3 OF § 138.4 ARE DONE AS OF 2026-09-04 AND § 142 IS THE PASS THAT DID THEM.**
This entry stays open for **step 4 only**, which needs hardware rather than code. Everything below
is still the reference for how a pad reaches a Unity game; the step list at the bottom says what
landed.

🧑 2026-09-04: *"idk how extensive controller support is"*, *"maybe add to todo that it can work
for fake controllers and shit too? haha or other brands"*, *"idk how controllers work so u figure
out how to do that"*.

**So this entry is the answer to "how do controllers work", written down once, plus what this
repository actually does today and what it costs.**

### 138.1 HOW A PAD REACHES A UNITY GAME, IN FOUR STEPS

1. **The pad enumerates as a USB HID device** carrying a vendor id, a product id and a report
   descriptor saying which axes and buttons it has.
2. **Unity's Input System matches that against its LAYOUT table.** There are hand-written layouts
   for the pads Unity knows: `XInputController` (every Xbox pad and anything in XInput mode on
   Windows), `DualShockGamepad` and `DualSenseGamepadHID` (PlayStation), `SwitchProControllerHID`.
   A match produces a device deriving from **`Gamepad`**, with `buttonSouth`, `leftStick`,
   `rightTrigger` and the rest in known places.
3. **A pad it does NOT know still gets a device**, auto-generated from the HID descriptor. ⚠️⚠️
   **THAT DEVICE DERIVES FROM `Joystick`, NOT FROM `Gamepad`.** It has axes and buttons with
   generic names (`trigger`, `stick`, `button3`) and no idea which button is "south".
4. **`Gamepad.current` only ever returns a step-2 device.** A step-3 device is never in it.

### 138.2 ⚠️⚠️ WHAT THAT MEANS HERE, AND IT IS EVERY CONTROLLER PATH AT ONCE

**Checked 2026-09-04: this repository contains no `InputSystem.RegisterLayout`, no `Joystick`
reference, and no input settings asset**, so it is entirely on Unity's default matching. Every
controller path in the game reads either `Gamepad.current` or a `<Gamepad>/...` binding path:

| Path | What it does with an unmatched pad |
|---|---|
| `LastInputDevice.Sample` | `Gamepad.current` is null, so `InputDeviceKind.Gamepad` is never set. **Every prompt in the game keeps showing keyboard keys** while the player holds a pad |
| `InputCatalogue` / `ScreenInputCatalogue` pad paths | `<Gamepad>/buttonSouth` and friends resolve to nothing. **No verb fires. The pad does nothing at all** |
| `ScreenFocus` menu navigation | same: the pad cannot move focus, so the front end is unusable |
| `Rumble` | returns at its `Gamepad.current == null` guard. Correct, and silent |
| The settings panel's **GAMEPAD** page | lists every action with `-` and rebinding cannot capture a press, because `PerformInteractiveRebinding` is restricted to `<Gamepad>` paths (`BeginRebind`'s own note). **The page is a list of controls that can never be bound** |

⚠️ **AND THE FAILURE IS COMPLETELY SILENT.** Nothing logs, nothing warns, no screen says
"controller not recognised". From the player's side the pad is simply dead, which is
indistinguishable from a broken cable or a broken game.

### 138.3 ⚠️ HOW BIG IS THIS ACTUALLY, BECAUSE THE ANSWER IS "SMALLER THAN IT SOUNDS ON WINDOWS"

**Most cheap third-party pads sold for PC ship in XInput mode**, or carry a physical X/D switch,
and in XInput mode Windows presents them as an Xbox pad and Unity matches them at step 2. So the
common case already works and this is not a claim that the game has no controller support.

**The ones that fall through are real and are exactly the ones in this room:**

- DirectInput-only pads, including a lot of no-name USB pads and older PC gamepads.
- USB adapters for original PlayStation, Saturn and Nintendo pads.
- Some Switch Pro clones and third-party arcade sticks.
- ⚠️ **On ANDROID the matching is different and weaker**, which matters because that platform ships
  too. Android reports pads through its own input API and Unity's Android backend maps common ones,
  but a Bluetooth pad from a market stall is a coin toss.

⚠️⚠️ **NOBODY HAS TESTED ANY PAD ON THIS PROJECT EXCEPT THE ONE ON THIS DESK.** That is the honest
state, and it is why this entry exists rather than a fix: the first job is finding out, not coding.

### 138.4 WHAT DONE LOOKS LIKE, IN THE ORDER THAT PAYS

⚠️ **NOT "WRITE LAYOUTS FOR EVERY PAD". That is a treadmill and Unity already lost it.** The cheap
wins are about telling the truth and about a fallback, in this order:

1. ✅ **DONE 2026-09-04: `InputLayer.ControllerWatch`.** A device that is plugged in and unmatched
   is no longer silent. It hooks `InputSystem.onDeviceChange` **and sweeps `InputSystem.devices` at
   startup**, because a pad plugged in before the game started never raises a change and is
   exactly the case nobody notices. A `Joystick` is the signature: that is what Unity's HID
   support produces for a device that declared a gamepad usage and matched no layout, and it is
   narrow enough not to report the keyboard and the mouse. It logs a warning with the
   manufacturer, product and interface, and `ConvertedSettingsPanel.HintFor` puts
   *"A controller was found that this game does not recognise, so it will not work."* on the
   **CONTROLS tab**, above the rebind sentence, because that is the screen somebody opens when
   their pad is not working.
   ⚠️ **A WARNING RATHER THAN AN ERROR**: nothing in the game is broken, and an error would fail
   every test run on a machine with a flight stick attached.
2. ✅ **DONE 2026-09-04, BY A DIFFERENT ROUTE, AND THE ROUTE THIS STEP ASKED FOR IS SHUT.**
   Registering a layout means winning `InputSystem.onFindLayoutForDevice`, and `InputManager` takes
   the **FIRST** callback that answers; Unity's own HID callback is registered during the Input
   System's static initialisation, which is triggered by the first touch of `InputSystem`,
   **including the touch that would register ours**. There is no order in which this game goes
   first, and out-scoring it with `RegisterLayoutMatcher` means beating a matcher HID builds per
   device from the vendor id, the product id and the usage.
   **`InputLayer.GenericPadBridge` therefore leaves the joystick alone and creates a `Gamepad`
   beside it**, pumped from `InputSystem.onAfterUpdate`. § 142.4 has the whole argument, the
   button order it guesses, the one-frame cost, and why it can be switched off.
3. ✅ **DONE, AND IT CAME FREE WITH STEP 2 EXACTLY AS THIS LINE PREDICTED.** The bridged device IS
   a `Gamepad`, so the GAMEPAD page, `LastInputDevice`, `ScreenFocus`, `Rumble`, every
   `<Gamepad>/` binding and the new `ControllerMapScreen` all work on it with no change anywhere.
   ⚠️ **The map is the better answer than the rebind page here**: a guessed order is wrong in a
   way a player has to SEE to fix, and § 142.3 is the screen that shows it.
4. ⚠️⚠️ **STILL OPEN, AND IT IS THE ONLY STEP THAT NEEDS A HUMAN.** A written list of what has
   actually been tested, with vendor and product ids, in [`../Attention.md`](../Attention.md) —
   **§ 14 of that file is the ask, written 2026-09-04**. One tested pad written down beats four
   assumed ones, and the bridge makes this matter more rather than less: it is asserted against a
   synthetic joystick, which proves the wiring and says nothing about the guess.

⚠️ **WHAT NOT TO DO: do not widen the `<Gamepad>` binding paths to `<HID>` in the input asset.**
`CLAUDE.md` § 4a's compile gate exists so a verb cannot ship without a pad answer, and a second
family of paths per action doubles the map while halving what `FindDuplicateBindings` checks,
which is the exact fault § 4a records `ResolveBindingIndices` fixing. **The fallback belongs in the
device layer, where one registration serves every existing binding.**

### 138.5 ⚠️ AND CROSSPLAY IS NOT AFFECTED, WHICH IS WORTH SAYING BEFORE SOMEBODY WORRIES

`CLAUDE.md` § 4a: **nothing about which device was used goes on the wire.** A pad, a thumb and a
keyboard all arrive at `InputIntent`, so a fallback layout is a purely local concern and **may not
move `NetSession.ProtocolVersion`**. The one place a device is a first-class fact is
`MatchmakingRules.PoolKey`, which bands the RANKED queue by device; an unrecognised pad reporting
as a keyboard player would put them in the wrong ranked band, which is a fairness argument for
fixing step 1 rather than a reason to touch the wire.

---

## 134 · THE BROADCAST PASS: AUTOPILOT, REPLAY, ULTIMATE INTRODUCTIONS, THE SHOVE THAT MEANT NOTHING, AND THE KEYBOARD ON THE PHONE ⚠️⚠️ OPEN, 2026-09-04, branch `abilities-rework`

🧑 2026-09-03, going into the nationals in General Santos City, asked for four things and then
added a fifth off a screenshot of the Android build:

1. Spectator autopilot that looks **intentionally directed** rather than merely automatic.
2. Manual instant replay that shows **the actual decisive event**, cleanly.
3. Six **distinctive non-verbal ultimate introductions**.
4. Attacker AI that stops **following players around to shove them** with no objective effect.
5. **"why the fuck does it have keybinds theres no keys in mobile"**, and
   *"ive never seen a mobile game say GRAB or lunge, usually it has an intuitive icon for it or
   the skill icon"*.

⚠️⚠️ **THE FIFTH IS THE ONE THAT WAS ALREADY SHIPPED AND WRONG, AND IT IS FIRST HERE FOR THAT
REASON.** The other four are work; that one was a defect a player met on a device. § 134.1.

---

### 134.1 ⚠️⚠️ THE TOUCH LAYER WAS PAINTING KEYBOARD KEYS, AND THE CAUSE WAS A FIELD TYPE

**What he saw.** An Android frame with `RUN`, `THROW`, `GRAB`, `JUMP`, `LUNGE`, `EMOTE` written on
six thumb controls, **`Q` and `E` on two more**, `ULT` on the ninth, `[X] PICK UP` over a tsinelas,
`[F1] NO COOLDOWNS: OFF` in the middle of the screen, and `PRESS F1 when ready` under the round
line. Two of those name keys on a keyboard the device does not have, on the one surface in the
game that exists **because** there is no keyboard.

⚠️⚠️ **THE CAUSE WAS NOT A BAD SET OF STRINGS. IT WAS THAT THE ONLY AVAILABLE ANSWER WAS A
STRING.** `InputCatalogue.VerbInput` carried a single `TouchLabel` field and `TouchHud.BuildButton`
drew it with `MenuKit.Label`. A field whose type cannot express a picture gets filled with a word
every time, and for the three Hero Strike slots the word each control was called by was its
keyboard key. This is `HeroAbility.Glyph`'s own argument one layer down: *"a lookup table keyed by
id is a second place to forget, and forgetting it compiles."*

⚠️⚠️ **AND `CLAUDE.md` § 4a'S COMPILE GATE WAS WORKING PERFECTLY THE WHOLE TIME.** *"A new `Verb`
does not compile until it has a pad binding and a thumb target."* It did have a thumb target. The
gate asked whether every verb was **reachable** on a phone and never whether it was
**intelligible** on one, and that gap is the whole of this defect. **The gate is now eight
questions rather than seven**: `VerbInput.Glyph` is a constructor parameter with no default, so a
verb cannot reach a phone again without somebody deciding what it looks like.

**What changed.**

| | |
|---|---|
| `UI/VerbIcons.cs` | New. Nine procedural glyphs baked the way `AbilityIcons` bakes the ability set: SDF coverage in a -1..1 square, white on transparent, tinted at the use site. Speed lines, a tsinelas on an arc, an open hand, a lift chevron, a forward thrust, a face, two skill plates and a star. |
| `InputCatalogue.VerbInput` | `Glyph` added as a constructor parameter with no default. `TouchLabel` **kept** and repurposed: it names the control in `TouchLayoutScreen` and in `GuidedTraining`, and is drawn on no button. `"Q"`, `"E"` and `"ULT"` became `"SKILL 1"`, `"SKILL 2"` and `"ULTIMATE"`. |
| `TouchHud.BuildButton` | Draws an `Image` at 54% of the target instead of a 34 pt label. The share is measured against `WoodCraft`'s keyline-rim-face construction, not picked. |
| `TouchButton.RefreshIcon` | ⚠️⚠️ **The three hero controls draw the LIVE ability's own icon**, not anything in `VerbIcons`. `docs/VISION.md` § 3 names three layers that *"must stay in step"*; the touch layer is a fourth surface for the same three powers and 🧑 asked for exactly this: *"or the skill icon"*. The `VerbGlyph` is the fallback for a seat with no kit. |
| `Hud.PressCue` / `Hud.MashVerb` | New. A key cap on a keyboard or a pad, **nothing at all on touch**. Eight prompt sites rewritten through them. |
| `TouchHud.Emphasise` / `TouchButton.SetHinted` | New. With the key cap gone, the prompt states the ACTION and **the button says which button**, by pulsing 9% at 1.6 Hz. A scale pulse rather than a colour one, because colour already means "you are pressing this". |
| `Hud` deck key caps | Empty on touch. The deck tile carries the ability icon and the thumb control now carries the same one, so the player maps them by picture. |
| `Hud` sandbox toggle | **Hidden on touch**, and it now has a touch equivalent rather than a gap. It reads `[F7] NO COOLDOWNS: OFF` (F1 until § 136.1) and the only way to change it is the key: on a phone it was a status readout for a switch nobody can reach. `TouchHud.BuildSandboxToggle` is the thumb half, on the layer's own canvas. § 134.9, closed by § 137. |
| `GuidedTraining.Key` | Names the CONTROL on touch, which is the one screen in the game where a word for a control is the content rather than noise. Sixteen lessons stopped teaching keys the device does not have. |

⚠️ **THE ONE PLACE A WORD SURVIVED IS THE TUTORIAL, AND THAT IS DELIBERATE.** Everywhere else a
phone prompt states the action, because the player already knows the game and only needs to know
what will happen. A tutorial is the opposite situation: it exists to teach which control does what.

#### ⚠️⚠️ AND THE FIX HAD A HOLE THAT ONLY THE CAPTURE SHOWED: THE FIRST FRAMES ON A PHONE

`Logs/shots-touch/touch-HeroStrike-short-wide-window-v4.png` has every button drawn as a picture
and **still shows `[F1] NO COOLDOWNS: OFF` and `Press [R] when ready`** in the middle of the
screen.

**The cause is `LastInputDevice.Current`, which defaulted to `KeyboardMouse`.** Its own comment
said why that was safe: *"on Android the first touch corrects it before anything is drawn."*
**The first touch is not before anything is drawn.** A phone player boots into the warmup window
and reads the round line, the ready prompt and the sandbox row for however long it takes them to
reach for the screen.

⚠️⚠️ **NOTHING DEPENDED ON THAT VALUE UNTIL THIS PASS, WHICH IS WHY THE COMMENT SURVIVED.**
`Hud.PressCue`, `Hud.MashVerb` and `GuidedTraining.Key` all branch on it now, so a keyboard
default means the first thing a phone player sees is exactly the defect 🧑 reported, in the one
window where those prompts are largest.

**It is seeded from `TouchHud.ShouldShow`**, which already answers the question honestly: the
platform define on Android and iOS, `Touchscreen.current != null` elsewhere. ⚠️ **A seed, not a
lock**: the next keyboard or pad press still moves it, so a phone with a Bluetooth keyboard
behaves and a touchscreen laptop still boots on keyboard.

⚠️ **THE PROBE CAPTURE CANNOT SHOW THE FIX**, because `InputSurfaceProbe` forces the layer on with
`TouchHud.ForceVisible` on a desktop editor where `Touchscreen.current` is null, so the seed
correctly stays on keyboard there. **This one needs the .apk on a handset to confirm**, and it is
in the handoff.

**Asserted by** `InputContractTests.NoTwoVerbsDrawTheSameTouchGlyph` and
`.NoTouchControlIsNamedAfterAKeyboardKey`. The second is a text rule (a label under three
characters, or one carrying brackets, is a key cap) rather than a list of forbidden strings, because
the next one to leak will be whatever key the next verb happens to be bound to.

---

### 134.2 ⚠️⚠️ THE ATTACKER SHOVE: FOUR FAULTS THAT PRODUCED ONE BEHAVIOUR

**Reproduced from the code before any change.** `AIController.SabotageTarget` at `71eeaf4`:

```csharp
float reach = 4.16f * Me.Sabotage;      // up to 4.16 m
...
float aim = Vector3.Dot(push.normalized, toTaya.normalized);
if (aim <= 0.0f) continue;              // 89.9 degrees is admitted
float score = aim * 2.0f - AiTuning.TagDistanceWeight * d;
if (who.HoldingSlipper) score += 1.0f;  // a bonus, not a requirement
```

| # | The fault | The number | What it looked like |
|---|---|---|---|
| 1 | **`aim > 0` is a direction test with no magnitude** | admits **89.9 degrees** off the line to the taya | A body moved 2.5 m for **4 cm** of closure |
| 2 | **Carrying a tsinelas was a `+1.0` score bonus** | — | `IsTaggable` REQUIRES one, so half of every sabotage set up nothing that could be punished |
| 3 | **The search radius was 2.6x the verb's reach** | **4.16 m** against `Balance.ShoveRange` **1.6 m** | The bot WALKS two and a half shove-lengths to set up a press it has not earned. **This walk is the following he reported.** |
| 4 | **Nothing projected the outcome** | — | "Toward the taya" and "into the taya's reach" were never told apart |

⚠️⚠️ **AND THE HEADER ABOVE THAT FUNCTION HAD SAID *"a rival worth shoving into the taya's
reach"* THE WHOLE TIME.** The comment described the intended rule and the body implemented a
proximity check with a sign test. That gap is why this read as *"meaningful sabotage"* in review
and as random harassment in play.

⚠️ **THE LOOSE BAR WAS ARGUED FOR, AND THE ARGUMENT WAS SOUND ABOUT THE WRONG THING.** The
comment beside `aim <= 0` reasoned that a cone *"would take it back to zero"* sabotages a match,
citing a real measurement: willingness read as `> 0` against a fixed radius produced **zero
sabotages over a whole match at Normal**. That measurement was about the SEARCH RADIUS being too
small for `Spacing`'s attacker separation, and it was answered by loosening the DIRECTION test.
Widening the radius to 4.16 m and keeping `aim > 0` bought opportunities by admitting bad ones.

#### The rule that replaced it

⚠️⚠️ **IT LIVES IN `Packages/com.tumbangpreso.core/Runtime/Sabotage.cs`, NOT IN `AIController`.**
`CLAUDE.md` § 4: the engine-free package holds *"every number arrived at by measurement rather
than taste"*, and engine-free is what lets them be *"asserted in a second instead of playtested
for an afternoon."* The old rule was observable only by watching a match. `SabotageTests` answers
eighteen cases in **27 ms**.

**Every bound is derived from the constants that resolve the shove. Nothing is typed in.**

| Quantity | Expression | At shipping constants |
|---|---|---|
| Shove travel | `Combat.ShoveDistance()` = `ShoveSpeed²/(2·Friction)` | **2.50 m** |
| Actionable reach | `max(Combat.LungeReach(), Balance.PunchRange)` | **2.30 m** |
| Taya response share | half of `Balance.ShoveStun` | **0.625 s** |
| **Danger radius** | reach + `Speed·DefenderSpeedScale·ShoveStun·0.5` | **5.46 m** |
| **Minimum closure** | `ShoveTravel × 0.60` | **1.50 m** |
| **Max approach** | `Balance.ShoveRange × 2` | **3.20 m** |
| **Max pursuit** | `MaxApproach / (Speed·AttackerSpeedScale) × 1.5` | **1.90 s** |
| Target cooldown | stated | **3.00 s** |

⚠️⚠️ **THE RESPONSE SHARE IS A HALF AND SPENDING THE WHOLE STUN WOULD HAVE MADE THE GATE
MEANINGLESS.** A taya moves at 5.06 m/s and the shove stun is 1.25 s, so the full window is
**6.33 m of closing** on a 14 m box: a danger radius of 8.6 m admits two thirds of the arena and
filters nothing. Half is the honest reading, because a taya spends the first half noticing and
turning, and `AiTuning`'s whole reaction model exists because this game refuses to ship a defender
with perfect information.

**A shove is taken only when ALL of these hold**, each with its own named veto so a diagnostic run
can print the distribution: the bot is an attacker; the target is another attacker; the target is
carrying; a taya exists and can act; the projected endpoint closes at least 1.50 m; the projected
endpoint lands inside 5.46 m of the taya; and no wall stands on the route.

⚠️ **THE GEOMETRY CHECK FILTERS BY COMPONENT, NOT BY LAYER, AND THAT IS THE PROJECT'S SHAPE.**
`ProjectSettings/TagManager.asset` has **no custom layers at all**: every body, prop, wall and
slipper is on `Default`. `Slipper.ResolveFlight` solves the same problem the same way. A player, a
slipper or the lata in the path is not an obstruction.

#### Pursuit, which is the other half of the complaint

- **A 1.90 s clock**, stepped every frame rather than on the think tick. `Me.Think` can be a fifth
  of a second, so an expiry that only fired on a re-plan would overrun by that much on every bot.
- **The bot walks to `SabotageRules.LaunchPoint`**, on the far side of the victim as seen from the
  taya, not at the victim's centre. ⚠️⚠️ **Walking at the centre is what made the loop**: it
  arrives beside them facing wherever the walk ended, fires into an arbitrary quadrant, and starts
  another approach.
- **The shove ends the plan whether or not it landed**, and puts that victim on a 3 s cooldown.
  Without this, every other bound is a stutter in the tail rather than an end to it.
- **Role change cancels everything.** `MatchRules.DefenderSlotFor` is `(round - 1) % 4`, so on
  `RoundStarted` the victim may now BE the taya. Cooldowns clear with it.
- **The plan in hand is re-projected, never trusted**, which makes the brief's whole cancellation
  list one rule: dropped tsinelas, retrieved tsinelas, taya knocked down, a wall arriving.

⚠️ **THE DIFFICULTY DIAL MOVED FROM A REACH TO A REACTION.** `Me.Sabotage` used to scale the
search radius, which is what let a weaker bot take a WORSE shove. It now gates `Reacted`, so a
weaker bot is slower to SPOT a legal opportunity. The brief's rule stated in code: difficulty must
not let low-quality bots perform meaningless shoves.

**Defender audit.** `PlanDefender` returns only `Idle`, `Reset`, `Intercept`, `Hunt`, `Cover` and
`Guard`; no defender path presses a shove, and `Combat`'s own note records why (*"the defender can
neither shove nor be shoved"*). `SabotageTests.ADefenderNeverReachesTheAttackerSabotageRule` holds
it from the other side. **Nothing about the tag, the punch or the lunge was touched.**

---

### 134.3 ⚠️ BASELINE: WHAT THE SPECTATOR AUTOPILOT ACTUALLY DID BEFORE THIS PASS

Read from `SpectatorDirector.cs` at `71eeaf4`, 573 lines, before any change.

**What it is.** A continuous per-frame score over four bodies (`ScoreSubject`), a `MinShotSeconds`
hold, and one camera solve: an orbit bearing around a focus point, at a distance driven by the
spread between the subject and one secondary. **Every shot in the game is that same solve at a
different bearing and distance.**

| Baseline failure | Where it comes from |
|---|---|
| **One shot vocabulary.** A retrieval, an ultimate, a knockdown and a quiet beat are the same orbit at different radii | `ComputeShot` is the only pose function |
| **No event model at all.** The score is recomputed every frame from live state | `ScoreSubject` has no notion of an event STARTING or ENDING |
| **Commitment is to a SUBJECT, never to an EVENT** | `MinShotSeconds` 2.4 s holds the person; nothing holds the play. A retrieval that runs 4 s can be cut away from at 2.4 |
| **No occlusion test whatsoever.** The only spatial guard is a box clamp | `ComputeShot` clamps x and z to `PlayableHalfX/Z + 1.5` and never asks whether anything is between the lens and the subject |
| **The camera can sit inside geometry.** Both maps are enclosed (house facades, viaduct pillars) | Same clamp. A bearing that lands on a pillar is taken |
| **An ultimate can begin off-screen.** It scores +5.0, which the retrieval's +6.0 plus the 1.25 switch margin can outrank | `ScoreSubject` |
| **The lata is only ever a secondary, and only when the subject is NOT taggable** | `FocusPoint`: a retriever is framed with the taya INSTEAD of the can, so the objective leaves the frame in the one shot the whole game is about |
| **Framing is centred, always.** Nothing leads except a 0.42 s velocity nudge on the focus point | `FocusPoint` |
| **Nothing re-establishes the axis after a cut.** The bearing alternates shoulders, which crosses the action line every other cut | `NewShot` |

⚠️ **THE THREE THINGS IT GETS RIGHT AND WHICH MUST SURVIVE**, all recorded in its header with
their reasoning: it CUTS rather than whip-panning past 6.0 m; it COMMITS rather than following a
continuous leader; and it is never completely still (`DriftDegPerSecond` 3.4). Those are kept
unchanged.

⚠️ **AND ONE THING THE BRIEF ASKS TO REVERSE, WHICH WAS A DELIBERATE DECISION WITH A REASON.**
`ManualTakeover` deliberately EXCLUDES the broadcast keys, and says why: *"pause, replay, mark and
recall are the operator working the GALLERY, not the camera, and a director should not be thrown
out for calling a replay of the shot it just covered."* The brief asks for mark, recall, replay and
pause to disengage autopilot. **Reversed as asked, and recorded here so the next session reads the
argument rather than rediscovering it.**

---

### 134.4 ⚠️ BASELINE: WHAT MANUAL REPLAY ACTUALLY SHOWED

Read from `SpectatorCamera.cs` at `71eeaf4` before any change.

**What it is.** A ring of `Texture2D` frames captured post-render at 854x480, one every 0.10 s, capped
at 70. Pressing the replay key takes the **newest 55 frames** and plays them once at 0.82x over the
whole screen, titled with whatever `PollHighlights` last saw.

| Measurement | Value |
|---|---|
| Sample interval | 0.10 s |
| Ring capacity | 70 frames = **7.0 s** |
| Clip length | `ReplaySeconds` 5.5 s = 55 frames, played at 0.82x = **6.7 s of screen time** |
| Frame size | 854 x 480 RGB24 |
| **Bytes per frame** | 854 × 480 × 3 = **1,229,760 B** |
| **Ring at capacity** | 70 × 1.23 MB = **86.1 MB of `Texture2D`** |
| Readback | one `ReadPixels` per 0.10 s, synchronous |

| Baseline failure | Why |
|---|---|
| ⚠️⚠️ **The clip is the last 5.5 seconds, NOT the last EVENT.** | `StartReplay` takes `_replayFrames.Count - wanted` from the END of the ring. Press it 4 s after a tag and the tag is 40 frames back with 15 frames of aftermath: the decisive moment is at the **start** of the clip or already gone |
| **The title can name a play the clip does not contain** | `RecentHighlightReason` expires against `ReplaySeconds`, so a reason 5.4 s old titles a clip whose first frame is 5.5 s old. One tenth of a second of honesty |
| **No frame carries anything.** `ReplayFrame` is one field: `Image` | No timestamp, no sequence, no marker, no reason, no slot |
| **86 MB of textures on a phone** | Android is the target platform this pass also has to ship. Nothing measured this before |
| **A synchronous `ReadPixels` every 0.10 s** | It is a GPU stall on the render thread, ten times a second, for the whole match, whether or not anybody ever presses replay |

⚠️ **THE THINGS IT GETS RIGHT.** It plays once and never loops (the *"loop every second"* report
was four triggers, not a loop). It is manual-only since 2026-08-27 and `DeadFeatureAudit` greps
this file for the names of the two deleted auto-replay constants. It covers the whole screen
because 🧑 asked: *"i want it to cover whole screen if i click it"*. **All three are kept.**

---

### 134.5 THE SHOT VOCABULARY, THE EVENT INTEREST MODEL AND WHAT THE AUTOPILOT DOES NOW

**Two files.** `Camera/SpectatorInterest.cs` is new and answers *what is the match about right
now*; `Camera/SpectatorDirector.cs` is rewritten in place and answers *where does the lens go*.
⚠️ **There is still exactly one autopilot and one spectator camera.** The brief forbids a second
version and `SpectatorCamera`'s header has forbidden it since it was written.

#### The event interest model

⚠️⚠️ **AN ORDERED ENUM OF NAMED BEATS, NOT A CONTINUOUS SCORE, AND THAT IS THE WHOLE CHANGE.**
`ScoreSubject` summed six live terms over four bodies every frame, so the leader moved several
times a second and the only thing holding the camera still was a 2.4 s lock on the SUBJECT. **A
hold on a person is not a hold on a play.** An event has a beginning, a duration and an outcome,
so it can be committed to; a score has none of the three.

| # | Beat | Expected | Commit | Shot |
|---|---|---|---|---|
| 1 | **Retrieval** with the taya closing | 4.2 s | 2.4 s | Chase inside 9 m, else retrieval two-shot |
| 2 | **Ultimate** winding up or active | 3.4 s floor, plus while `IsActive` | 2.4 s | Wide if footprint ≥ 3 m, else hero shot |
| 3 | **Lata hit** or knockdown | 2.6 s | 1.8 s | Objective |
| 4 | **Tag** landed, or a lunge charging | 2.8 s | 2.4 s | Recovery / chase |
| 5 | **Slipper landed** within 4 m of the can | 2.0 s | 1.8 s | Objective |
| 6 | **Downed** or stunned | 2.6 s | 2.4 s | Recovery |
| 7 | **Reset** channel | 2.4 s | 2.4 s | Defender |
| 8 | **Throw prep** | 2.5 s | 2.4 s | Objective |
| 9 | **Quiet** | 5.0 s | 2.4 s | Establishing |

⚠️ **EVERY DURATION IS MEASURED AGAINST SOMETHING AND SAYS WHAT.** The retrieval is a sprinting
crossing of the 14 m box at `Speed × AttackerSpeedScale` = 2.53 m/s plus a beat; the reset is the
longest can (`Combat.ResetChannelFor` is 1.79 s on BOYBEN) plus the stand-up; the throw is
`Balance.ChargeFullTime`. **A duration picked by feel is one the next person retunes by feel.**

⚠️⚠️ **`OutcomeGrace` IS 1.15 s AND IT IS THE "DO NOT LEAVE DURING THE OUTCOME FRAME" RULE.** A
retrieval stops being true on exactly the frame it resolves, which is the frame the viewer has
been waiting for. A condition-driven camera cuts on that frame and shows the run while hiding the
result. The beat stays true for a beat after it stops.

⚠️ **THE EVENTS COME OFF THE GAME'S OWN SIGNALS**, as the brief requires: `Lata.UprightChanged`,
`MatchDirector.Scored`, `RoundDirector.Tagged`, `HeroAbilitySystem.UltimateStarted`,
`Carrier.IsCharging` and `Carrier.ChannelRatio`, `CombatVerbs.ObservedLungeCharge`. **The one
thing with no event is a tsinelas coming to rest**, so it is polled on a 0.20 s scan, which is
what `Hud.UpdatePickupPrompt` already does for the identical problem.

#### The nine compositions

⚠️⚠️ **EACH IS A DIFFERENT PLACE TO STAND, NOT A DIFFERENT RADIUS.** `BaseBearingFor` decides the
angle from the geometry of the play and `Solve` decides the distance and the height per shot type.

| Shot | Bearing | Distance | Height |
|---|---|---|---|
| **Retrieval two-shot / Chase** | 90° off the axis between the runner and the chaser | 5.5 + spread × 0.70 | 2.35 m |
| **Objective** | from behind the can, looking back at the person | 4.2 + spread × 0.55, capped 9 | **1.25 m** (near can height) |
| **Ultimate wide** | 118° off the caster's facing | 9.5 + spread × 0.8 | **6.4 m** |
| **Ultimate hero** | 118° off the caster's facing | 4.5 | **1.55 m** (looking up) |
| **Defender** | from the can out through the taya | spread-driven | 3.1 m |
| **Recovery** | 70° off the axis between the body and the threat | 4.6 + spread × 0.55 | 1.85 m |
| **Quiet establish** | along the map's LONG axis | 13.5 | **7.2 m** |

⚠️⚠️ **THE HERO-SPECIFIC ULTIMATE FRAMING IS ONE RULE, NOT SIX.** Dante's brief asks the camera
out of the fissure path, Zack's asks for vertical room, Sean's asks not to stare at empty ground,
Nemu's asks not to shoot straight down. **All four are "stand off the axis the power travels
along"**, which is the caster's own facing, and the wide-versus-hero choice comes from
`HeroAbility.TelegraphRadius`, the authored answer to "how much floor does this cover". A per-hero
table would be six places to forget, which is `HeroAbility.Glyph`'s argument.

⚠️ **THE QUIET SHOT LOOKS DOWN THE LONG AXIS BECAUSE THE BOX IS 8.6 BY 13.0.** A shot down the 13
is a street; a shot across the 8.6 is a wall.

#### Composition rules that are now enforced rather than hoped for

- **The lata is in the retrieval frame.** The old `FocusPoint` framed a retriever with the taya
  INSTEAD of the can. A two-shot now pulls toward the midpoint of the chaser and the objective, so
  all three are in the picture.
- **`Headroom` 0.35 m.** A camera aimed exactly at a chest puts the head against the top of the
  frame at close distances.
- **Off centre by construction.** Aiming past the subject toward what the shot is also about
  places the subject off centre and the objective in frame in one operation.
- **Lead 0.42 s** off `CharacterMotor.Velocity`, unchanged, so a shove or a dash is in it.
- **The shoulder flips on a re-frame and holds across a cut.** Alternating on every cut crosses
  the action line every other time; holding it across a cut to a new beat lets the new geometry
  re-establish the axis.

#### Pose validation, which did not exist at all before

`ValidatePose` refuses a candidate that is inside geometry (`OverlapSphereNonAlloc`, 0.45 m
clearance) or that cannot see the main subject and the secondary or objective (`RaycastAll`).
**Six bearings are tried at 60° apart before falling back**, and the fallback is 11 m over the
focus looking down, which is safe by construction because nothing in either map is up there.

⚠️ **A PLAYER CROSSING THE LENS IS NOT AN OCCLUSION.** The filter drops `CharacterMotor`,
`Slipper` and `Lata` hits, the same way `Slipper.ResolveFlight` and `AIController
.ShoveRouteIsClear` do, because every layer in this project is `Default` and bodies move.

⚠️ **RE-SOLVING BEATS ABANDONING.** Occlusion is a property of a POSE, not of a play: a retrieval
does not stop being the most interesting thing in the match because one bearing looks at a house.

**Counters for the capture log:** `OccludedPoseRejections`, `SafePoseFallbacks`, `Cuts`, and
`Diagnostic`, which is the model's own sentence about the last decision.

---

### 134.6 THE REPLAY MARKERS AND THE EVENT WINDOW

**Replay stays manual.** Nothing added a trigger. `StepBroadcastKeys` still routes the one bound
key, the autopilot has no hands, and `DeadFeatureAudit` still greps this file for the two deleted
auto-replay constants.

#### Every frame carries what it is

`ReplayFrame` was one field, `Texture2D Image`. It now carries `CapturedAt`, `Sequence`,
`Highlight`, `Reason`, `Slot` and `EventAt`.

⚠️⚠️ **THE MARKER IS STAMPED ON THE FRAME AT CAPTURE, AND THAT IS WHAT MAKES EXPIRY FREE.** A
marker in a side list has to be aged against a ring that drops its oldest frame ten times a
second, and the two go out of step the first time anybody changes the capacity. **A marker that
lives on the frame it describes cannot outlive it**, which is the brief's *"expire markers when
their frames leave the buffer"* made structural rather than remembered.

#### What gets marked

| Event | Source |
|---|---|
| Lata knockdown | `MatchDirector.Scored` / `Lata.UprightChanged` |
| Tag | `MatchDirector.Scored` |
| Sabotage | `MatchDirector.Scored` |
| **Retrieval under pressure** | polled edge: taggable attacker inside `ChaseGap` 9.0 m of the taya |
| **Ultimate impact** | `HeroAbilitySystem.UltimateStarted`, titled with the ultimate's own name |
| Decisive score event | `MatchDirector.Scored` |

⚠️⚠️ **TWO OF THE SIX SCORE NOTHING, WHICH IS WHY NEITHER WAS EVER MARKABLE.** A retrieval made
under a closing taya and an ultimate landing award no points, so `MatchDirector.Scored` never
mentioned them, and they are among the best clips the game produces: getting a tsinelas out from
under a closing taya is the play `docs/VISION.md` opens by calling the whole point of the sport.

⚠️ **THE PRESSURE MARKER FIRES ON THE EDGE ONLY.** A retrieval is true for seconds; a marker per
frame would fill the buffer with a hundred markers describing one play, and `NewestHighlightIndex`
would pick its last frame every time.

⚠️ **THE SEAT IS CARRIED SO THE OVERLAY CAN NAME WHO.** Both `Scored` and `Tagged` knew it and it
was being thrown away.

#### The window

**3.5 s before the marked frame, 1.3 s after it.** The lead-in is measured: an attacker crosses
the box at 2.53 m/s so the run into a tag is about three seconds, and `Balance.ChargeFullTime` is
2.5 s, so a full charge fits too. ⚠️ **A replay that starts at the impact shows the result and
hides the decision**, and the decision is what is worth watching twice.

⚠️ **THE FALLBACK IS THE NEWEST 5.0 s AND IT IS NEVER EMPTY.** With no marker in the buffer the
clip is the last interval; under twelve frames it refuses and says so; under four frames of window
it refuses and says so.

⚠️⚠️ **THE FRAMES AFTER THE CLIP ARE KEPT NOW, WHICH IS A BUG FIX NOBODY HAD REPORTED.** The old
body called `_replayFrames.Clear()`, emptying the whole ring, so **the buffer restarted cold after
every replay and a second replay was impossible for ten seconds.** Only the frames before the clip
are dropped; everything after it stays live and the ring keeps filling behind the overlay.

#### Buffer cost, measured

| | before | after |
|---|---|---|
| Frame | 854 x 480 RGB24 | **640 x 360 RGB565** |
| Bytes per frame | 1,229,760 | **460,800** |
| Capacity | 70 frames = 7.0 s | **100 frames = 10.0 s** |
| **Held at capacity** | **86.1 MB** | **46.1 MB** |

⚠️ **THE WINDOW HAD TO GROW AND THE MEMORY HAD TO SHRINK AT THE SAME TIME.** 3.5 s of lead-in plus
1.3 s of lead-out plus however long the operator takes to press is more than 7.0 s of history, and
Android is a shipping platform for this build. 640 x 360 is exactly half of 720p; RGB565 is two
bytes a pixel instead of three and the banding is invisible on a moving toon-shaded picture watched
once at 0.82x.

#### The overlay

It said `INSTANT REPLAY · TAG` and nothing else. It now carries **REPLAY**, the event, the
responsible player when known, a progress bar along the bottom edge, and one line reading
`LIVE PLAY CONTINUES · <key> OR ESC TO RETURN`.

⚠️⚠️ **THE EXIT IS THE ONE THAT MATTERS MOST, BECAUSE THE CLIP COVERS THE WHOLE SCREEN.** 🧑 asked
for full screen in 2026-08-27 and that is right; a spectator who does not know Escape works is
watching a box they cannot leave, on a broadcast. `CLAUDE.md` § 6.2's fourth question is exactly
this. ⚠️ **The player is named only when the marker carries a seat**, because inventing a name for
the fallback interval would be `docs/VISION.md` § 3's *"a screen that teaches the wrong key"*
applied to a scoreboard.

**It plays once, never loops, refuses a restart while playing, exits on the replay key or Escape,
alters no gameplay time, no scoring and no network state, and is unreachable from the autopilot.**

---

### 134.7 THE ULTIMATE INTRODUCTIONS

**One owner: `UI/UltimatePresentationDirector.cs`.** It subscribes to
`HeroAbilitySystem.UltimateStarted`, which is raised from `PlayUltimatePresentation` — already the
one funnel every cast passes through **exactly once per peer**.

⚠️⚠️ **"MUST NOT FIRE FROM PREVIEWS, REJECTED CASTS OR UNAVAILABLE PRESSES" IS SATISFIED BY WHERE
THE HOOK IS, NOT BY A CHECK INSIDE IT.** `Cast` returns `Refused`, `Cooling`, `NotCharged`,
`CannotAct` or `Missing` on every path that is not a real cast, and only `CastOutcome.Cast` reaches
`PlayCastConfirm`. `MatchRpc.BroadcastAbilityCast` skips `_nm.LocalClientId`, so the host never
receives its own announcement.

⚠️ **THE DUPLICATE WINDOW IS STILL NEEDED AND IT IS A NETWORK PROPERTY.** A client that predicted
a cast and then received the host's confirmation runs `ApplyNetworkCast`'s non-authoritative
branch, which forces the effect through **on purpose** (*"a host-approved effect must never vanish
merely because one screen counted a timer a frame differently"*). Right for the effect, wrong for a
title card. 0.35 s, keyed on the seat.

**What the player gets.** A compact bottom-left card, **0.78 s**, sliding in over 0.14 s and out
over 0.18 s: hero name in the hero's accent, ultimate name at 34 pt, the ultimate's own
`AbilityIcons` glyph, and a hero-specific motif strip. **No camera cut, no time change, no input
lock, no crosshair hidden, nothing over the target, `blocksRaycasts` off.**

⚠️ **0.78 s IS PICKED AGAINST `HeroAbility.Windup`, WHICH IS 0.4 s.** A card shorter than the
wind-up names the caster and leaves before an opponent can act on it; one much longer is still on
screen while the payload lands, competing with the thing it announced.

⚠️ **BOTTOM-LEFT AND 560 UNITS WIDE, BOTH MEASURED.** The width is `DEVOURING SEANCE` at 34 pt
(~340 units) plus a 92-unit icon plus three 22-unit margins. **A fraction of the window would not
be a size**: `AspectSafeCanvas` scales on the short axis, so a percentage is 1920 units at 4:3 and
2250 on his own short wide window, which is what § 100 cost the sign-in screen. Bottom-LEFT keeps
it out of the ability deck's column at every aspect ratio, and the deck is the one thing a player
reads while deciding what to answer an ultimate with.

**The six motifs, and why a shape rather than a tint.** `CLAUDE.md` § 6.5: *"a shape difference
survives a photograph and a colourblind player; a fill difference does not."* Six cards differing
only in accent are one card. Dante a fault line, Cheska a crystal ridge, Sean a rising flame, Zack
a bolt staircase, Nemu a funnel trough, Phaister a coven ring.

**The six names are read off the kits**, never out of a table in the presentation file: `TITAN
FISSURE`, `GLACIAL NOVA`, `SUPERNOVA`, `THUNDERSTRIKE`, `DEVOURING SEANCE`, `GRAND COVEN`.

**No new audio and no new VFX.** The cast cue, the hero theme, the column, the weather and the
shake all already fire from `HeroAbilitySystem` and are untouched.

---

### 134.8 ⚠️ REJECTED APPROACHES, AND WHY

| Rejected | Why |
|---|---|
| **Returning the touch label from `Hud.KeyLabel` on touch** | It would print `[GRAB] PICK UP`, and since this pass the buttons draw ICONS: "GRAB" is a string that appears nowhere on the screen it points at. The cap is dropped and the button pulses instead |
| **A tenth thumb control for the sandbox toggle** | A developer switch that cannot exist in a networked session, bought with permanent screen space. `CLAUDE.md` § 6.2: weigh an addition by what the player has to hold in their head |
| **Folding `VerbGlyph` into `AbilityGlyph`** | `AbilityIcons.LabelFor` reports a JOB vocabulary that `HeroPresentationTests` asserts is unique per ability. JUMP is not a job an ability has |
| **Sharing `AbilityIcons`' SDF primitives instead of transcribing them** | They are `private` and its `Size` and `Stroke` are its own. Exporting them makes one file's feathering constant part of another file's contract, and the next person retuning an ability icon silently retunes every thumb control |
| **A hard-coded shove distance beside the combat constants** | `Combat`'s own header forbids it for impulses; a stale DECISION constant is worse than a stale distance because it produces a bot that looks stupid rather than a wrong number |
| **Spending the whole shove stun in the danger radius** | 8.6 m on a 14 m box. Arithmetically defensible, useless as a filter, and it assumes a taya with perfect information |
| **Queueing ultimate introductions** | Two ultimates inside a second is a real Hero Strike moment; a queue titles the second one after it has finished. `docs/VISION.md` § 2 rule 2 already says one at a time |
| **`DontDestroyOnLoad` for the introduction canvas** | Every screen-space canvas that outlives a scene is a candidate for § 6.2b's fourth trap, which is what `PlayerNameplate` drawing across the account form actually cost |

---

### 134.9 ✅ CLOSED 2026-09-04: A PHONE IN PRACTICE CAN TURN COOLDOWNS OFF

The `[F1] NO COOLDOWNS` row is hidden on touch (§ 134.1) because F1 is the only way to change it
and a phone has no F1. **That removes a keybind leak and leaves a genuine gap**: a mobile player in
practice cannot use the sandbox.

⚠️ **IT IS NOT FIXED BY MAKING THE PLATE PRESSABLE.** The HUD canvas has no `GraphicsRaycaster`
(§ 113), so a uGUI button there *"would draw correctly, raycast nothing and read as a dead
control"*. The honest options are a tenth control on `TouchHud`'s canvas (rejected, § 134.8) or a
row in the pause menu, which is a screen that already raycasts. **Done looks like**: a mobile
player in practice can toggle the sandbox from a surface that already exists.

✅ **BUILT 2026-09-04 as `TouchHud.BuildSandboxToggle`, and § 134.8's rejection was reversed on its
own terms rather than ignored.** That entry rejected *"a tenth thumb control for a developer
switch"*, and the answer is that it is not a tenth control in a match: `PracticeSandbox.Allowed` is
`!NetAuthority.IsNetworked`, so the control **does not exist** in any networked session and costs a
player in a real match nothing to hold in their head, which is the test `CLAUDE.md` § 6.2 asks for.
The pause-menu option was the other candidate and is worse: it puts a switch two presses deep
behind a screen that stops the world, for a thing the player wants to flip WHILE trying a power.

- **On `TouchHud`'s canvas, which is the half that makes it work at all.** `TouchHud.Build` goes
  through `MenuKit.BuildCanvas`, so the raycaster, the aspect-safe scaler and the focus path arrive
  with it. This is the § 113 objection answered rather than dodged.
- **Top left**, the one corner no thumb rests in: the stick owns the bottom left, the verb arcs the
  right, and the look area is the right-hand 55 per cent.
  ⚠️⚠️ **BUT 268 UNITS DOWN, NOT IN THE CORNER, AND THE FIRST VERSION DREW STRAIGHT ACROSS THE
  SCOREBOARD.** `Logs/shots-runtime/Eskinita.png` caught it: NO COOLDOWNS OFF over the top two
  rows of the SCORES panel, hiding a seat's name and role. *"No thumb rests here"* and *"nothing
  is drawn here"* are different questions and only the second one was asked. This is `CLAUDE.md`
  § 6.2b's fourth row, **the fourth time a piece of chrome has had to be taught about something
  added after it**, and the first across two different canvases: `TouchHud` and `Hud` are separate
  canvases, so nothing either owns could have noticed.
- **236 by 148**, so it clears the 144-unit thumb floor on its SHORT axis. `CLAUDE.md` § 7's 1519
  failures were all controls sized against their own artwork.
- ⚠️ **POLLED EVERY FRAME, NOT SET AT BUILD**, because a session becomes networked after the canvas
  exists: the layer is installed per match and a player can host from a lobby it was already built
  under. `RefreshSandbox` runs before `Update`'s layout early-returns for the same reason, so the
  switch cannot show a stale ON in the first frames of a multiplayer match. 🧑: *"make sure this
  doesnt leak into actual game or shti"*.
- ⚠️ **A WORD RATHER THAN A GLYPH, AND THAT IS NOT § 134.1 REPEATING ITSELF.** That fault was
  painting KEY NAMES on a device with no keys. `VerbGlyph` is a closed list of what a power does to
  the world (`VISION.md` § 3) and there is no glyph for "suspend the cooldown rules"; inventing one
  for a developer switch would be the lookup-table-to-forget that `CLAUDE.md` § 4a exists to stop.
- **The desktop row stays hidden on touch.** One switch drawn twice is two controls for one state,
  and the pair drifts the first time either is retuned.

---

### 134.10 ⚠️ OPEN: THERE IS NO REDUCED-EFFECTS OR FLASH-REDUCTION SETTING TO RESPECT

The brief asks the ultimate introduction to respect reduced-effects and flash-reduction settings.
**`GameSettings` has neither.** It carries volume, sensitivity, invert-Y, fullscreen, rumble,
anti-alias, v-sync, render style, slipper highlight, AI difficulty and match format, and nothing
about photosensitivity.

**So it was answered by construction instead**: the introduction has no full-screen layer, no
strobe and no white frame, so there is nothing for such a setting to reduce. ⚠️ **That is not the
same as the feature existing.** `docs/FUTURE.md` Phase 16 is where an accessibility pass belongs,
and the loudest thing in this game is not this card: `AbilityShowcaseProbe` exists because Zack's
Thunderstrike once blew **62.8 per cent** of a frame to white. **Done looks like**: one setting,
read by `AbilityVfx`, `SkyEvent`, `Hitstop` and this card together.

---

### 134.12 ✅ CLOSED 2026-09-05: THE REPLAY CAPTURE IS ASYNCHRONOUS, BOUNDED AND POOLED

`CaptureReplayFrame` calls `Texture2D.ReadPixels` **ten times a second for the whole match**,
whether or not anybody ever presses replay, and a `ReadPixels` blocks the render thread until the
GPU catches up.

**It is 2.7x cheaper than it was** because the frame went from 854 x 480 RGB24 to 640 x 360
RGB565 (1,229,760 B to 460,800 B), and that is a real reduction rather than a rounding. **It is
not a fix.** `AsyncGPUReadback` is the right answer: the request is queued, the callback arrives a
frame or two later, and the ring is written from the callback instead of from the render loop.

⚠️ **IT WAS NOT DONE IN THIS PASS ON PURPOSE.** An async readback changes the ORDER frames arrive
in relative to the marker stamping in `CaptureReplayFrame`, and getting that wrong produces a
clip whose marker is on the wrong frame, which is silent and is exactly the class of fault
§ 134.4 was written about. **Done looks like**: `AsyncGPUReadback` with the marker resolved
against `CapturedAt` rather than against arrival order, and a measurement of the frame time with
and without it on a phone.

⚠️ **AND THE CAPTURE ONLY RUNS FOR A SPECTATOR.** It is on `SpectatorCamera`, so a player never
pays for it. The cost lands on the machine running the stream, which at the nationals is the one
machine that most needs its frame rate.

✅ **DONE 2026-09-05, AND THE ORDERING WORRY ABOVE WAS THE RIGHT ONE TO HAVE.** The answer is not
to resolve the marker against `CapturedAt` afterwards; it is that **the ring slot is reserved at
REQUEST time and only the picture arrives late.** A callback that APPENDED its frame would order
the buffer by whenever the driver got round to it, stamp it with a completion time, and hand the
marker to whichever frame happened to land next, three separate corruptions of a clip whose whole
value is that it contains the play. Reserving the slot keeps the capture time, the sequence and the
marker exactly where the synchronous version put them.

**What that needed:**

- ⚠️ **A `Pending` flag on the frame.** A pending frame is IN the ring (or the order and the
  timestamps are the driver's) and is NOT in a clip (its texture holds the previous tenant's tenth
  of a second). `ReadyFrameCount` is the difference, and "the buffer is still warming up" is now an
  honest answer on a machine that is behind.
- ⚠️⚠️ **A CAP ON OUTSTANDING REQUESTS.** `MaxOutstandingReadbacks` is **4**, sized off the sample
  rate and the pipeline depth rather than picked: a readback lands two to three frames after the
  request and `ReplaySampleInterval` is six frames at 60 Hz, so one is outstanding at a time on a
  healthy machine. Past four, the honest reading is that the GPU cannot keep up and the requests
  are a queue rather than a buffer. **Exceeding it drops the capture** rather than deferring it,
  because a replay is a moving picture watched once at 0.82x and one missing tenth of a second is
  invisible.
- ⚠️⚠️ **A GENERATION COUNTER, WHICH IS THE WHOLE OF "NO CAPTURE AFTER THE SESSION IS GONE."** A
  readback callback is a closure the driver invokes on a later frame, and `Destroy` on a
  `Texture2D` is deferred to the end of a frame, so "the camera is gone" and "the callback has
  stopped arriving" are separated by however far behind the GPU is. `OnDestroy` bumps the counter
  **before** anything is destroyed and then calls `WaitAllRequests`; every callback in flight
  compares unequal and returns having touched nothing.
- ⚠️ **A TEXTURE POOL.** The old path allocated a `Texture2D` per capture and destroyed it per
  eviction: **3,600 native allocations and 3,600 frees in a four-round Classic set**, all of the
  same 460,800 bytes. The pool is bounded by the ring, so the memory ceiling is exactly what
  § THE REPLAY BUFFER already states.
- ⚠️ **THE MEMORY DID NOT MOVE, AND THAT NEEDED THE SCRATCH TARGET RATHER THAN THE READBACK.** A
  `RenderTextureFormat.RGB565` blit converts on the GPU for free, so the bytes coming back are
  already two per pixel. Reading ARGB32 back and packing it here would have traded a GPU stall for
  a 230,400-iteration CPU loop ten times a second, which is not obviously the better deal.
- ⚠️ **THE SYNCHRONOUS PATH SURVIVES AS THE FALLBACK.** `SystemInfo.supportsAsyncGPUReadback` is
  false on some older Android drivers and Android is a shipping platform here (§ 130.5 is the last
  thing that ANR'd it), so a replay that silently stopped working there would be a feature deleted
  for a whole platform in the name of a frame time nobody had measured on it.

**MEASURED, `ReplayCaptureProbe` on `eae4e96`, macOS/Metal, 120 calls each at the shipped
640 x 360 RGB565:**

| path | mean | p95 | p99 | worst | over 2 ms |
|---|---|---|---|---|---|
| `ReadPixels` (before) | **0.734 ms** | 1.750 | 1.750 | **2.066 ms** | 1 |
| `AsyncGPUReadback` (after) | **0.035 ms** | 0.250 | 0.250 | **0.158 ms** | **0** |

**21x cheaper on the mean, 7x on the percentiles, 13x on the worst call**, and the once-per-session
first call went from **7.2 ms to 0.5 ms**. Priced over a match: a four-round Classic set is 3,600
captures, so **2.64 s of CPU spent on the render thread becomes 0.13 s**, and the frames that used
to carry a 2 ms stall carry none over 2 ms at all. 120 readbacks landed, 0 failed.

⚠️ **THE NUMBERS ARE THIS MACHINE'S AND THE RATIO IS THE FINDING.** `CLAUDE.md` § 7: a wall-clock
result depends on how busy the machine is, which is why the assertion in the probe is a floor a
broken build crosses (the async worst call must beat the synchronous one) rather than a bound tuned
to a laptop.

⚠️⚠️ **AND THE PROBE'S OWN FIRST RUN MEASURED THE WRONG THING, WHICH IS WORTH THE PARAGRAPH.**
It reported the async path's worst call as **147.7 ms** against `ReadPixels`' 4.6, while its p95
and p99 were **0.250 ms against 0.750**. The 147 ms was the FIRST `AsyncGPUReadback.Request` of
the session allocating its readback buffers, once, and the warm-up loop only spun frames rather
than making real calls, so it warmed the renderer and not the thing under test. **A one-time
initialisation cost standing as a path's "worst call" is a measurement of initialisation wearing a
per-call statistic's name.** Both paths get five untimed calls now, and **the warm-up cost is still
printed on its own line**, because a hitch on the first capture of a match is a real thing that
happens once and hiding it would be the opposite fault.

**The measurement is `ReplayCaptureProbe`** (`Logs/replay-capture.txt`), which performs both calls
at the shipped resolution and format and reports mean, p95, p99, worst and long-call counts for
each. ⚠️ **It measures the two CALLS rather than toggling the shipped component**, deliberately: a
switch on `SpectatorCamera` would be a settable static that survives a scene change, which is
exactly what § 145.3's roster exists to catch, added for a test's convenience.

---

### 134.13 ✅ THE CASTER RAIL IS THREE CHARACTERS ON A ROW THAT ALREADY EXISTED

The brief asked for a spectator rail carrying four names, scores, the current taya, tsinelas
state, ultimate readiness and downed state. **The scoreboard already carried the first three**,
and it is drawn for a spectator by `Hud.EnterSpectatorMode`, whose own note explains why: *"the
timer and the scoreboard stay: those are facts about the MATCH, and they are exactly what
somebody watching wants."*

⚠️⚠️ **SO A SECOND PANEL WOULD HAVE PUT FOUR NAMES AND FOUR SCORES ON SCREEN TWICE**, which is
the *"do not reproduce the player HUD four times"* the same brief forbids two lines later. The
addition is one cell on each existing row, three characters wide:

| position | means | glyph |
|---|---|---|
| 1 | carrying a tsinelas | `T` or `·` |
| 2 | ultimate banked and ready | `U` or `·` |
| 3 | down or stunned | `X` or `·` |

⚠️ **EVERY POSITION ALWAYS PRINTS SOMETHING**, so the three columns line up down the board and a
caster reads a COLUMN rather than four strings. A cell that only drew the letters that applied
would be unreadable at a glance, for the same reason `_scoreMarks` is a fixed width: *"a cell
that hides itself lets the score column stop being a column."*

⚠️ **SPECTATOR ONLY.** A player already reads their own tsinelas and their own ultimate meter off
the deck, in far more detail than three characters.

⚠️⚠️ **AND IT FREEZES DURING A REPLAY**, which the brief names: *"during replay, freeze or clearly
label values so live cooldowns do not appear to describe recorded footage."* `Hud` asks
`SpectatorCamera.Replaying` rather than tracking its own flag, so there is one answer to the
question and it cannot drift.

---

### 134.14 ✅ ESKINITA WAS THE ONLY MAP THE GEOMETRY CHECK DID NOT GATE, AND IT HAD TEN FINDINGS

**Bayan Plaza reports 0 findings and Ilalim ng Tulay reports 0. Eskinita reported ten**, as
`(informational)`, and had done for long enough that nobody read them. That is `docs/TODO.md`
§ 124.11's rule in a different colour: a permanently amber map teaches every reader to skim the
map report.

⚠️ **ESKINITA IS ALSO THE MAP THE TOURNAMENT PLAYS ON.**

| Finding | What it actually was | Answer |
|---|---|---|
| Four `Sasakyan_*` bodies floating **0.263 m** over `Kalsada` | parked cars hovering a hand's width above the road | set down |
| `Sasakyan_2_W/door` floating 0.195 m | a child of one of those cars | followed its parent |
| Two `Sampay_*` floating 1.66 m and 2.47 m | washing lines strung between poles | `AirborneByDesign`, with a reason |
| Two `Quad`s floating 0.898 m | 18 x 94 m and 60 x 75 m backdrop planes | `AirborneByDesign`, with a reason |
| `Sampay_0` standing **0.79 m** from the can spawn | the same washing line, flattened to two dimensions | resolved by the excuse |

⚠️⚠️ **THE LAST ROW LOOKED LIKE THE ONLY GAMEPLAY FAULT AND IS NOT ONE.** `CheckLataIsClear`
opens with `if (p.Airborne) continue;`, so excusing the washing line clears it from that check as
well — and that is the correct reading rather than a silencing. The rule exists because *"Ilalim
ng Tulay had a trip hazard centred on"* the can; **a wire strung 2.47 m overhead is not something
a retriever can walk into.**

⚠️⚠️ **AND THE FIXER'S FIRST RUN IS WORTH RECORDING, BECAUSE IT REPORTED SUCCESS AND CHANGED
NOTHING.** Measuring each car's gap from the whole object's renderer bounds returned *"already
resting (gap 0.000 m)"* on all four while the check went on reporting 0.263 m, because the
parent's bounds also contain **a ground-level contact shadow sitting flat on the road**. The
shadow was resting and the car was not. It solves from the `body` child now, which is the piece
the check names.

⚠️ **NOTHING HERE TOUCHED GAMEPLAY GEOMETRY.** The cars are `Dressing`, outside the chalk, and
the check reports `box  0 solid object(s) inside the chalk` before and after.

⚠️ **`EskinitaRestingFix` IS A SCRIPT AND NOT A HAND EDIT**, for `BayanPlazaMonumentFix`'s reason:
the scene is an imported `.tscn`, so a hand edit is a diff nobody can review and a re-import
undoes. It is idempotent and it refuses rather than guesses.

---

### 134.15 ⚠️ OPEN: THE MAP SOURCES IN `Asset_Sourcing.md` § 7 ARE NOT IN THE REPOSITORY

🧑 asked to *"refine maps a bit more with the new assets we have"* and to confirm where they are
written up. **They are `docs/Asset_Sourcing.md` § 7, and none of them are downloaded.**

| Source | State |
|---|---|
| Quaternius Ultimate Buildings / Modular Streets / House Interior / Furniture / Nature | CC0, **not fetched**. `tools/fetch_asset_sources.py` does not cover them |
| Plastic Monobloc Chair (Poly Haven) | CC0, **not fetched**. ⚠️ The map already places an `env_monobloc_chair`, so this is a QUALITY replacement rather than a missing prop |
| Jeepney (Sketchfab) | CC BY, **needs a signed-in account**. `Attention.md` § 11.2 |
| Manila Street ambience (Freesound) | CC BY, **needs a signed-in account**. `Attention.md` § 11.1 |

⚠️⚠️ **WHAT IS IN THE REPOSITORY IS THE KENNEY LIBRARY, AND IT IS 583 MODELS.**
`Assets/TumbangPreso/Art/models/kits/` holds `roads`, `city`, `commercial`, `industrial`, `town`,
`forest`, `graveyard`, `train`, `factory`, `car`, `food` and `footwear`. **That is what "the new
assets we have" actually means today**, and § 134.14 is a pass over what the maps built from it
already have wrong rather than a pass adding more of it.

**Done looks like**: either the two logins in `Attention.md` § 11 happen and the jeepney replaces
the distant north `van` per `Asset_Sourcing.md` § 7.1, or both are written off there rather than
left looking pending.

---

### 134.16 ⚠️ THE CONTROLLED DIAGNOSTIC RAN AND REPORTED ZERO SHOVES, AND THE FIRST READING OF THAT WAS UNREADABLE

`AiDiagnosticProbe` at 1x, Normal difficulty, one round each in Classic and Hero Strike, three
bots per round:

```
sabotage ledger:
  seat 3  plans 0  shoves 0  longest pursuit 0.00s  time pursuing 0.0s  last veto None
  seat 2  plans 0  shoves 0  longest pursuit 0.00s  time pursuing 0.0s  last veto None
  seat 0  plans 0  shoves 0  longest pursuit 0.00s  time pursuing 0.0s  last veto None
  TOTAL  plans 0  shoves 0  longest pursuit 0.00s  time pursuing 0.0s
  bounds: max pursuit 1.90s  max approach 3.20m  danger radius 5.46m  min closure 1.50m
```

⚠️⚠️ **`last veto None` COULD NOT BE READ, AND THAT WAS THE PROBE'S FAULT RATHER THAN THE
RULE'S.** `default(SabotageProjection)` has `Veto = None`, which is the value that means *the
shove was worth taking*, so **a bot that never projected anything and a bot whose last projection
was perfect printed the same word.** A measurement that cannot tell "never considered" from
"considered and fine" is not a measurement, and zero shoves is exactly the reading that needed
explaining: the rule this pass replaced carried a comment recording *"ZERO sabotages over a whole
match"* as the symptom of a bound being wrong.

**So the probe now counts the veto distribution and the number of candidates projected**
(`AIController.SabotageVetoes`, `SabotageProjectionsMade`). A run reporting mostly
`OutOfApproachRange` is a bot that was never near anybody, which is a fact about the match; one
reporting mostly `EndpointStaysSafe` is the projection doing its job; one reporting
`VictimNotVulnerable` is three attackers who were not carrying. **The old output called all three
of those "0".**

⚠️⚠️ **AND THE NEW APPROACH RANGE IS WIDER AT NORMAL THAN THE OLD ONE WAS, WHICH IS THE OPPOSITE
OF WHAT "ZERO SHOVES" SUGGESTS.** The old radius was `4.16 * Me.Sabotage`, and `Me.Sabotage` is
**0.35 at Normal**, so the old search radius was **1.46 m** there: *below* `Balance.ShoveRange`
1.6. The new one is a flat **3.20 m**. This pass tightened the DIRECTION and the OUTCOME and
loosened the RANGE at the difficulty the diagnostic runs at.

⚠️ **WHAT ZERO PROBABLY IS.** The conjunction a shove needs is genuinely uncommon: another
attacker **carrying**, within 3.2 m, positioned so the push closes 1.5 m, landing inside 5.46 m
of a taya who can act. An attacker who has just picked their tsinelas up is running OUT of the
box while the taya is near the can, so the shover has to be further out than the victim and
crossing them. That happens a few times a match, not a few times a round.

#### The second run, with the distribution, and what it actually says

```
  candidate shoves projected: 0
    (nothing was ever projected: no rival came within 3.20 m while a shove was
     affordable and off cooldown)
```

⚠️⚠️ **SO THE RULE WAS NEVER EXERCISED IN PLAY AT ALL, AND THAT IS A DIFFERENT FACT FROM "THE
RULE REFUSED EVERYTHING".** Not one candidate reached `SabotageRules.Project` in two 40 second
rounds. The vetoes are all zero because nothing got as far as being vetoed: no second attacker
came within 3.20 m during a frame when this bot's shove was off cooldown and it had 27 stamina.

**That is a fact about the match, not about the projection**, and it is consistent with three
things that were already true before this pass:

* `AiTuning`'s `Spacing` is **0.60 at Normal** and its whole job is to push the three attackers
  apart, which the old code's own comment named as the reason sabotage was rare.
* An attacker who has just picked their tsinelas up is running OUT of the box while the taya is
  near the can, so the shover has to be further out than the victim AND crossing them.
* Attackers sprint constantly, and `Balance.ShoveStaminaCost` is 25 of a 60 bar.

⚠️ **THE THREE GATES ARE STILL CONFLATED IN ONE LINE.** The message names range, affordability
and cooldown together because they are three early returns before any projection. **Done looks
like** three counters instead of one sentence, over a full eight round match at Astig
(`Me.Sabotage` 0.85) rather than two rounds at Normal (0.35), so the zero can be attributed to
one of them rather than to all three.

⚠️⚠️ **AND THE UNIT TESTS ARE WHAT PROVE THE RULE FIRES**, which is the right division of labour:
`SabotageTests` has eighteen cases in 27 ms including
`SabotageIsSelectedWhenACarryingVictimWillBePushedIntoReach`, `AnAngledShoveIsTakenWhenItStill
ClosesMeaningfully` and `StandingOnTheLaunchPointProducesAMeaningfulShove`. **A behaviour that
happens a few times a match cannot be measured by a forty second round**, and that is exactly why
`CLAUDE.md` § 4 puts the rule in the engine-free core where it can be asserted instead.

⚠️ **WHAT THIS DOES NOT SHOW IS ALSO WORTH STATING PLAINLY: nobody has yet watched a bot take a
good shove in a live match.** The tail behaviour 🧑 reported is gone by construction (there is a
1.90 s pursuit clock stepped every frame, and the approach radius is half what it was), but the
positive case is proven by test and not yet by observation.

---

### 134.17 ⚠️⚠️ THE CAPTURE PROBE HUNG THE RUN AND WOULD HAVE GONE GREEN OVER AN EMPTY FOLDER

`NationalsShowcaseProbe.Frame` was written as the obvious three lines:

```csharp
yield return new WaitForEndOfFrame();
ScreenCapture.CaptureScreenshot($"{dir}/{name}.png");
```

**Both of them are wrong in `-batchmode`, independently, and neither says so.**

1. ⚠️⚠️ **`WaitForEndOfFrame` NEVER RESUMES.** There is no rendering loop to reach the end of, so
   the coroutine parks forever. The run sat with a static log for several minutes and had to be
   killed; the log's last line was an ordinary round-start message and nothing in the output
   pointed at the capture.
2. ⚠️⚠️ **`ScreenCapture.CaptureScreenshot` WRITES NOTHING.** There is no swap chain to capture.
   It fails silently, so even without the hang the run would have finished with an empty folder.

⚠️⚠️ **AND THE PROBE'S OWN ASSERTION WOULD NOT HAVE CAUGHT IT.** It read
`Assert.Greater(shot, 100)`, and `shot` is a loop counter that increments whether or not a file
appears. **A capture probe that counts its own intentions is `docs/TODO.md` § 96 and § 124.11 with
pictures**: green about something it was not doing. It counts
`Directory.GetFiles(OutDir, "showcase_*.png")` now.

⚠️⚠️ **THE FIX WAS TO USE THE PATH THAT ALREADY WORKS RATHER THAN TO REPAIR THIS ONE.**
`GameplayShots.Render` is `internal` and takes an output directory as of this pass, and every
paragraph in it is a fault somebody already paid for:

| What it does | What skipping it costs |
|---|---|
| resolves the HDR target through an sRGB one | every shot a stop and a half too dark |
| draws the UI with a SECOND, ungraded camera | the scoreboard photographs as **pure black** (measured: Godot (49, 25, 11), this harness (0, 0, 0)) |
| creates the render target BEFORE the layout pass | every font rasterised small and then enlarged. 🧑: *"why do ur fonts look blurry in ur pics?"* |
| restores every layer it moved | the next shot in the run is of a differently-layered scene |

**Writing a second capture path would have been writing all four of those bugs again.**

---

### 134.18 ⚠️⚠️ WHAT THE FIRST REAL CAPTURE FOUND, AND ALL FOUR WERE INVISIBLE IN THE SOURCE

`CLAUDE.md` § 6.1: *"show, do not describe."* § 6.2a: *"a green layout probe is not a good
screen."* **The suite was 344 green and all seven checks passed while every fault below was
live.** The run wrote 219 frames and a decision log; four things came out of looking at them.

| # | What the capture showed | Cause |
|---|---|---|
| 1 | The caster rail **clipped at the card edge**: row two read `T-` where it should read `T··` | The scoreboard card is 520 units and the row already held a rail, a worst-case name cell, a 132-unit badge, a 64-unit score and four 14-unit gaps. Adding 44 more pushed the last cell past the wood |
| 2 | `beat Ultimate  NOT SEEN` in the coverage report, on the one run that **forces all six** | The probe wrote `Intent.Set(Verb.Ultimate, true)` onto a seat `AIController.Update` rewrites every frame. **Not one ultimate cast.** The eight frames named `ult_dante` were eight frames of ordinary play |
| 3 | **53 cuts across 77 seconds**, a cut every 1.45 s against a 2.4 s minimum | `ValidatePose` ran every frame. It found the held bearing occluded, swung 60 degrees, `FlyToShot` saw a pose that had moved further than `CutDistance` and cut, then `DriftDegPerSecond` walked the bearing back into the obstruction. **28 occluded poses re-solved and 53 cuts are the same number**: the camera was cutting because it was arguing with itself |
| 4 | `shot Defender  NOT SEEN`, `beat Reset  NOT SEEN` | Honest: the taya never channelled a reset in the covered window. Not a fault, and the report says so rather than hiding it |

**Fixed:**

1. `EnterSpectatorMode` widens the scoreboard by `CasterStateWidth + ScoreRowSpacing`. ⚠️ Widened
   THERE rather than at build time, because a player never draws the cell and their board must
   not grow to leave room for something they will not see.
2. The probe disables the seat's `AIController` before pressing, and holds the verb for four
   frames rather than one. ⚠️ **Disabling the bot is the honest hook**: `CLAUDE.md` § 4 says a bot
   presses the same buttons a human does and there is no second path, so taking the seat over and
   pressing IS the human path, while reaching into `HeroKit.CastUltimate` would be the second
   path that invariant forbids. The press is held for four frames because
   `HeroAbilitySystem.Aim` reads an EDGE and `CharacterMotor.FixedUpdate` samples on the physics
   step, so a one-frame press can fall between two steps.
3. `SpectatorDirector.RevalidateSeconds` is 0.4 s. ⚠️ **A camera does not need 60 Hz occlusion**:
   bodies move at 2.5 to 5 m/s and walls do not move at all, so two and a half checks a second is
   well inside the rate at which the answer can change, and it costs a fifth of the raycasts.
4. The coverage report samples the beat INSIDE the ultimate frame loop. The first version
   recorded it once, after all eight frames and the settle, by which point a 0.4 s wind-up was
   long over: it would have printed `NOT SEEN` even on a run where the camera covered it
   perfectly.

⚠️⚠️ **AND THE PROBE'S OWN ASSERTION CAUGHT NUMBER 2**, which is the point of writing one:
`Assert.IsTrue(beatsSeen.Contains(SpectatorBeat.Ultimate), "the autopilot never recognised an
ultimate, which is the one beat this run forces and therefore cannot legitimately miss.")`

---

### 134.19 · THE CAPTURE, AND WHAT FOUR RUNS OF IT MEASURED

`Logs/shots-showcase/` holds **177 frames and a decision log** from the final run, one Hero Strike
match on Eskinita under the autopilot, all six ultimates forced, a manual replay called on a
marked event.

| | run 1 | run 2 | run 3 | run 4 |
|---|---|---|---|---|
| **cuts** | 53 | 47 | 35 | **33** |
| seconds per cut | 1.45 | 1.6 | 2.2 | **2.3** |
| **safe-pose fallbacks** | 0 | 10 | 0 | **0** |
| occluded poses re-solved | 28 | 23 | 13 | **15** |
| ultimates that cast | 0 | 6 | 6 | **6** |
| frames written | 219 | 219 | 219 | **177** |

⚠️⚠️ **2.3 SECONDS PER CUT IS THE BROADCAST MINIMUM, WHICH IS THE NUMBER THIS WAS AIMING AT.**
`MinShotSeconds` is 2.4 s and the reasoning is in its own doc comment: *"under about two seconds a
viewer has not finished reading the frame before it changes, and a director who cuts faster than
that is editing rather than covering."* Run 1 was cutting at 1.45 s, which is **editing**.

**Coverage on the final run:** 8 of 9 beats and 5 of 9 shots.

| Not seen | Why |
|---|---|
| `beat Reset` | The taya never channelled the can back onto its mark inside the covered window. Honest, and the report says so rather than hiding it |
| `shot Defender` | Follows from the above: it is the reset beat's composition |
| `shot RetrievalTwoShot` | Every retrieval in this run had the taya inside `ChaseGap` 9.0 m, so all of them composed as `Chase`. **That is the model working**, not a gap |
| `shot UltimateHero` | All six forced ultimates carry a telegraph radius at or above 3.0 m, so all six chose `UltimateWide`. The hero shot is for a tight-footprint ultimate and the roster does not currently have one |
| `shot Pov` | Deliberately never emitted by the autopilot. See `ShotType.Pov` |

⚠️ **THREE OF THOSE FIVE ARE THE SYSTEM CHOOSING CORRECTLY AND ONE IS THE MATCH NOT PRODUCING THE
EVENT.** Only `Reset` and `Defender` are genuinely uncovered, and they are the same beat.

**What the frames show, checked by eye rather than asserted:**

* the quiet establishing shot looks down the long axis of the street with the whole arena in frame
* the caster rail reads `·U·`, `TUX` and so on, three characters, unclipped, on every row
* the ultimate card reads **SEAN** in his own red above **SUPERNOVA** at 34 pt, with the starburst
  motif, clear of the controls overlay
* the autopilot status line names the play rather than the mode: `AUTOPILOT · retrieval · DANTE ·
  move to take over`

⚠️ **ONE THING WORTH A SECOND LOOK BY A PERSON, AND IT IS NOT NEW.** The ultimate impact's
chromatic aberration is very strong at spectator range: `showcase_0130_ult_sean.png` is fringed
across the whole frame. That is `HeroAbilitySystem`'s existing *"weight of the press"* pass, which
🧑 asked for by name (*"i want all ults to feel like they hit harder"*) and which scales by
`falloff` from the CAMERA. **A spectator camera is not a player camera**, and this pass did not
touch that scaling. `docs/VISION.md` § 2 rule 5 is the standard it should be judged against.

---

### 134.20 ⚠️ NOT DONE: THE REPLAY OVERLAY IS NOT IN THE CAPTURE, AND BATCH MODE IS WHY

The showcase run writes fourteen frames named `showcase_*_replay.png` and **every one of them is
live gameplay**. The replay never started.

⚠️⚠️ **THE CAUSE IS THE HARNESS, NOT THE FEATURE.** `SpectatorReplayCapture.OnRenderImage` is
where a frame enters the ring, and an image effect only runs when its camera actually renders. In
`-batchmode` there is no game view being drawn, so the spectator camera renders **only when the
probe explicitly calls `cam.Render()`**, which is about three times a second rather than the ten
`ReplaySampleInterval` asks for. `StartReplay` refuses below twelve frames and says so, so the
press was consumed and answered with a toast.

⚠️ **THE SHIPPING PATH IS UNAFFECTED AND THAT IS WORTH STATING PLAINLY.** In a real player the
camera renders every frame, `OnRenderImage` fires every frame, and the `_captureReplayFrame` flag
gates it to 10 Hz exactly as it always has. **This pass changed the frame FORMAT, the frame SIZE
and the METADATA, not the mechanism that fills the ring.**

**What IS verified about the replay, and how:**

| Claim | Evidence |
|---|---|
| the buffer holds the window plus reaction time | `BroadcastPassTests.TheReplayBufferHoldsTheWholeWindowPlusAnOperatorsReactionTime`, arithmetic off the constants |
| it stays under 50 MB | `.TheReplayBufferStaysUnderFiftyMegabytes`, 43.9 MB computed |
| exactly one trigger, and it is a key | `.ReplayHasExactlyOneTriggerAndItIsAKeyPress`, one call site |
| the autopilot cannot reach it | `.TheAutopilotCannotReplayPauseOrChangeTime`, source text |

**What is NOT verified: the overlay itself.** Nobody has seen `REPLAY · TAG · ZACK`, the progress
bar, or the exit line on a screen. ⚠️ `CLAUDE.md` § 6.2a is exactly about this gap: *"a green
layout probe is not a good screen ... the probe asks whether the screen is a screen; the picture
asks whether it can be read."*

**Done looks like** one of:

1. **A human presses the replay key in the Windows build** and looks at it. One minute, and it is
   the honest test.
2. **The probe drives the camera at capture rate**, calling `cam.Render()` on the same 0.10 s
   cadence the ring wants for a couple of seconds before pressing. That makes the batch harness
   fill the ring the way a player would, at the cost of a slower capture.

⚠️ **DO NOT "FIX" IT BY LOWERING THE TWELVE-FRAME FLOOR.** That floor is what stops a replay
playing three frames of nothing, and lowering it to make a probe pass would ship a worse feature
to make a test green.

---

### 134.11 · VERIFIED

**Every number below is off a run on this machine on 2026-09-04, at the HEAD this section ships
with.** ⚠️ `CLAUDE.md` § 7: read `total` and `failed` out of the `.xml`, never the exit code,
because a crash and a green run both come back as 0 and a third state writes a well-formed file
saying `result="Passed" total="0"`.

| Suite | Result |
|---|---|
| `dotnet test Core.Tests` | **483 passed, 0 failed**, 266 ms. Includes the 18 new `SabotageTests`, which answer on their own in 27 ms |
| EditMode, whole suite | **344 passed, 0 failed**, `Logs/tests-134f.xml`. Was 329 before this pass; the 15 new ones are `BroadcastPassTests` (13) and `InputContractTests` (2) |
| `Checks.RunAll`, all seven | **OK**, one launch. `headless`, `arena`, `map geometry`, `audio cues`, `scene scripts`, `input surface`, `shader warmup` |
| `MapGeometryCheck`, Eskinita | **findings 0**, down from **10**. Bayan Plaza 0, Ilalim ng Tulay 0 |
| PlayMode, targeted | 27 cases, **23 passed, 4 failed**, all four pre-existing. See below |
| PlayMode, `NationalsShowcaseProbe` | **1 passed**, 177 frames written and verified on disk |
| PlayMode, `AiDiagnosticProbe` | **2 passed**, Classic and Hero Strike, with the sabotage ledger |
| PlayMode, the touch photograph | **1 passed**, `Logs/shots-touch/*-v4.png` |
| Windows player | **SUCCEEDED**, 793 MB, 54 s, `C:\Users\Matthew\Desktop\TumbangPreso-Unity\TumbangPreso.exe` |
| Android player | **SUCCEEDED**, 228 MB .apk (1928 MB staged), 483 s, `C:\Users\Matthew\Desktop\TumbangPreso-Android\TumbangPreso.apk` |

⚠️ **BOTH PLATFORMS ARE FROM THE SAME COMMIT**, which `CLAUDE.md` § 4a requires whenever shared
runtime code moves. `NetSession.ProtocolVersion` did **not** move (it is 23 and
`InputContractTests` asserts it), but the camera, the HUD, the input layer and the AI all did.

⚠️⚠️ **THE FOUR PLAYMODE FAILURES ARE PRE-EXISTING AND DOCUMENTED, AND THIS PASS TOUCHES NONE OF
THE FILES THEY MEASURE.**

| Failure | Why it is not this batch |
|---|---|
| `CarryTests.AHeldSlipperStaysOnTheArmThroughMovementAndAMissingAnchor` | § 93, on its fourth sample, all outside the bound. *"Not a flake"* |
| `SteeringTests.MouseAimedMovementIsRelativeToTheBody` | § 130.14, *"pre-existing, deterministic, measured at HEAD without this batch's teardown and identical to eight significant figures"* |
| `SteeringTests.AMovementAimedSeatTurnsToFaceItsDirection` | same fixture, same cause |
| `SteeringTests.TheSteeringFrameByFrameIsWrittenOut` | same fixture; it is the per-frame writeout § 130.14b added to diagnose the other two |

⚠️ **`git diff HEAD` IS EMPTY FOR `CharacterMotor.cs`, `Carrier.cs`, `PlayerInputReader.cs` AND
`Slipper.cs`**, which are the four files those two fixtures exercise. ⚠️ **They were not re-run at
HEAD for this batch**, so that is an argument from the diff and from § 93 and § 130.14 rather than
a fresh measurement.

#### What the new tests actually hold

| Test | The claim |
|---|---|
| `TheAutopilotCannotReplayPauseOrChangeTime` | neither autopilot file names `StartReplay`, `ToggleBroadcastPause`, `SetBroadcastScale`, `RequestBroadcastScale`, `ProbeReplayRequest`, `Time.timeScale` or `Hitstop.` |
| `TheAutopilotTouchesNoGameplayState` | nor `InputIntent`, `AddScore`, `MatchRpc`, `ServerRpc`, `HostResolve` or a collider |
| `ReplayHasExactlyOneTriggerAndItIsAKeyPress` | exactly one call site into `StartReplay` |
| `TheRetrievalOutranksEveryOtherBeat` | the nine beats are ordered as `docs/VISION.md` § 0 requires |
| `ThereAreNineDistinctShotCompositions` | nine shot types, and each is branched on in the solver |
| `TheReplayBufferHoldsTheWholeWindowPlusAnOperatorsReactionTime` | buffer 10.0 s against a 4.8 s window, three seconds of headroom |
| `TheReplayBufferStaysUnderFiftyMegabytes` | 43.9 MB, computed from the constants and the texture format |
| `AllSixHeroesHaveTheirOwnNamedUltimate` | the six names, read off the kits, unique |
| `EveryHeroHasItsOwnUltimateMotif` | six distinct silhouettes |
| `TheUltimateIntroductionNeverInterruptsPlay` | the card names no camera, no timescale, no cursor lock, and does not block raycasts |
| `TheIntroductionPlaysNoAudioOfItsOwn` | no `NetCue`, no `AudioSource`, no `AudioCues` |
| `NoTwoVerbsDrawTheSameTouchGlyph` | nine controls, nine pictures |
| `NoTouchControlIsNamedAfterAKeyboardKey` | no touch label under three characters and none carrying brackets |

⚠️⚠️ **`TheUltimateIntroductionNeverInterruptsPlay` FAILED ON ITS FIRST RUN AND THE FAILURE WAS THE
TEST'S**, which is worth recording because the fix went the other way. It forbade the string
`enabled = false` and caught **the director switching off its own canvas when the card finishes**,
which is the one thing that method exists to do. A rule that catches correct behaviour is a rule
the next person deletes rather than reads, so it forbids `Camera` instead: the brief's hard line is
*"do not cut the gameplay camera"*, and a class that never names the type cannot cut one.

⚠️ **AND THE SOURCE-TEXT CHECKS NEEDED A COMMENT STRIPPER BEFORE ANY OF THEM WERE HONEST.**
`CLAUDE.md` § 3 asks for the reasoning in prose above the code, so `SpectatorDirector`'s header
*states* that nothing in the file calls `ToggleBroadcastPause`, `StartReplay` or
`SetBroadcastScale`. A naive `Contains` on the raw file sees all three names and fails the file for
documenting the rule it obeys. `BroadcastPassTests.Code` strips comments first.

#### The touch pass, measured

| | before | after |
|---|---|---|
| Thumb controls drawing a WORD | **9 of 9** | 0 |
| Thumb controls drawing a keyboard KEY | **3** (`Q`, `E`, `ULT`) | 0 |
| Prompts printing a key cap on touch | **8** | 0 |
| Deck tiles printing a key cap on touch | 3 | 0 |
| Hero controls drawing the live ability icon | 0 | **3** |

#### The AI pass, measured

| Bound | Expression | Value |
|---|---|---|
| Shove travel | `Combat.ShoveDistance()` | 2.50 m |
| Actionable reach | `max(LungeReach, PunchRange)` | 2.30 m |
| Danger radius | reach + half a stun of closing | 5.46 m |
| Minimum closure | 60% of travel | 1.50 m |
| Max approach | `2 x ShoveRange` | 3.20 m (was up to **4.16**) |
| Max pursuit | one approach at attacker speed, plus a turn | 1.90 s (was **unbounded**) |
| Target cooldown | stated | 3.00 s |



---

## 133 · ONE FONT IS DOING EVERY JOB, AND IT IS A DISPLAY FACE ⚠️⚠️ OPEN, 2026-09-03, NEXT SESSION'S BRIEF

🧑 2026-09-03, after three rounds of chasing blurry text on the TAB tray: **"I think the problem is
we use the same font for everything"**, and the direction that follows from it:

> *"I think darumadrop can be our main font, in next chat ask it to use a font that would fit with
> darumadrop as well as overhaul the ui of everything in lobby as well as login, with this work in
> progress logo which i will attach"*

⚠️⚠️ **HE IS RIGHT AND § 132.8 IS THE EVIDENCE, ARRIVED AT FROM THE OTHER END.** That entry chased
"the text seems very blurry" through a stale capture frame, a wrapping row, a clipped box and a
soft render, and the last thing left standing was this: **`DarumadropOne-Regular.ttf` ships ONE
WEIGHT**, so every `FontStyle.Bold` in the front end is legacy `Text` drawing each glyph twice at
an offset. That is a smear rather than a weight, and it is worst at `MenuKit.MinReadableUnits`,
which is where most of the words in this game are. **Forty sites in
`Assets/TumbangPreso/Runtime/UI/` alone.**

⚠️ **AND THE DEEPER VERSION OF IT IS THAT A DISPLAY FACE IS DOING BODY-TEXT DUTY.** Darumadrop is a
rounded, high-personality display font: it is exactly right on `CHOOSE YOUR HERO`, on a pennant, on
an ability name. It is carrying four-line ability descriptions, settings rows, chat and the sign-in
form as well, and a face chosen to be looked at is not a face chosen to be read.

### 133.1 What the next session is being asked for

1. **A second font that pairs with Darumadrop**, for body and UI text, leaving Darumadrop as the
   display face. It needs a real bold so nothing has to be faked. ⚠️ **Licence first**: it goes in
   a public repo inside a competition entry, so SIL OFL or equivalent, and the licence file ships
   beside it the way `Resources/Vfx/SOURCES.txt` does.
2. **The lobby UI, overhauled.** § 118, § 119 and § 121 are the standing entries; § 118.1 lists
   eight things that still read badly.
3. **The login screen, overhauled.** § 100 and § 121.4 are its history, including *"This shhit is
   horrible bro the art is cut off"*.
4. **The work-in-progress logo.** ⚠️⚠️ **HE SENT IT ON 2026-09-03 AND SAID TWICE THAT IT IS NOT
   FINISHED**: *"not done yet"*, *"its work in progress"*. It is a hand-drawn `TUMP` wordmark, a
   thick deep-red outline over pale peach letters, with a chartreuse blob and an orange dripping
   flourish behind it. **It arrived in the chat and is NOT in this repository.** Nothing can be
   built against a thumbnail: the first move of § 133 is getting the file committed under
   `Assets/TumbangPreso/Art/ui/brand/` so it can be sampled and drawn rather than described.

   ⚠️⚠️ **THE COLOURS ARE FINAL AND THEY ARE THE LOGO'S. THIS IS SETTLED, DO NOT RE-OPEN IT.**
   🧑 2026-09-03: *"the colors are final, ask it to use the same colors as logo"*. Two swatch
   strips ship with the image and the named colours are **Honey Quartz, Chartreuse, Persimmon,
   Khaki and Army**, plus the deep red the wordmark is outlined in. **The art is work in progress;
   the palette is not.**

   ⚠️⚠️ **SO `CLAUDE.md` § 6.4'S PALETTE MOVES, AND IT MOVES IN THE SAME COMMIT AS THE CODE.**
   That section currently names carved wood `#31190B` `#5A2F14` `#8B5227` `#1D0E06`, cream
   `#F5E6C8`, amber `#FFBA00` and warm ink `#1C0F06` as **the** palette. After this pass that is
   the OLD palette, and a rule that names the wrong colours is worse than no rule: it is
   `docs/TODO.md` § 5's drift, in the one file every session reads first. **Rewrite § 6.4's
   palette list, keep its ban and keep its receipts.**

   ⚠️ **THE BAN ITSELF IS UNTOUCHED AND THE NEW SET DOES NOT TEST IT.** No blue, no navy, no cold
   grey, in any layer. Persimmon, Honey Quartz and the deep red are warm; Chartreuse and Army are
   yellow-green and olive, which are warm-side greens and not cold greys. **Nothing here is a
   licence to relax § 6.4**, which 🧑 had to state six separate times.

   ⚠️ **READ THE HEXES OFF THE COMMITTED FILE. DO NOT TYPE THEM IN BY EYE AND DO NOT SAMPLE A CHAT
   THUMBNAIL.** The swatches carry their own hex labels printed on them. This is
   `tools/build_input_glyphs.py`'s rule: it reads a pack's palette and stops on a colour it has
   never seen rather than guessing, and that is why the glyph recolour has never silently drifted.

   ⚠️ **AND `UiTheme` IS THE ONE PLACE.** § 6.4's own receipt is `UiTheme.Ink` being a near-black
   NAVY for the entire life of that file with nobody seeing it, because one constant put navy on
   every word in the front end. Check the new set the same way it says to:
   `grep -rnE 'Hex\("[0-9a-f]{6}"\)' Assets/TumbangPreso/Runtime/UI/UiTheme.cs` and read the
   channels, rather than looking at a screenshot.

⚠️⚠️ **AND HE ASKED FOR IT TO BE DESIGNED WITH THE `game-ui-design` SKILL RATHER THAN BY EYE.**
🧑 2026-09-03: *"but ask it to use the game design skill ... to design shit in the same theme and
vibe and color as our work in progress new logo"*, with the install line
`npx skills add https://github.com/omer-metin/skills-for-antigravity --skill game-ui-design`.
**Invoke it before writing the screens, not after.** It is already available in the Claude Code
session as `game-ui-design`; the `npx` line is how it is added anywhere it is not.

⚠️ **THE SKILL IS A METHOD AND `CLAUDE.md` IS STILL THE LAW.** Where its general game-UI advice
disagrees with § 6.4's colour ban, § 6.5's `WoodCraft`/`PaperCraft` surfaces, or `VISION.md` § 6's
*"his UI art is the design system"*, this repository wins. It is being brought in for the thing it
is good at, which is hierarchy, readability at speed and controller and thumb reachability, on a
front end that has been rejected three times for exactly those.

### 133.2 What that session must read before touching anything

- `CLAUDE.md` § 6.5, which is why the front end is drawn in his art's own geometry, and
  `WoodCraft`/`PaperCraft`'s closed lists of surfaces. **A font swap is not licence to redraw a
  control.**
- `CLAUDE.md` § 6.4. No blue, no navy, no cold grey, in any layer.
- § 121.8, still open and still 🧑's call: whether `PaperKit.Caption`'s 16 is too small. **A new
  body face changes the measurement that question was asked against**, so it should be answered
  with the new font rather than before it.
- `MenuKit.MinReadableUnits` is 18 and `AspectRatioProbes` fails anything under it. A face with a
  larger x-height reads bigger at the same number; **the floor is about physical legibility, not
  about the number**, so re-measure rather than assume it can drop.

### 133.3 ⚠️ The trap, named in advance

**`MenuKit.Font` is one static and every screen in the game reads it.** Swapping it is a one-line
change that repaints the entire front end at once, and every layout in this repository was measured
against Darumadrop's metrics: `UiRows.Cap`, `PaperKit.Caption`, `HeroTaglineHeight`, the 1.35x box
ratio in `AbilityInspectPanel`, `Phase10Tests`' 67-character description budget and
`MenuKit.Fit`'s shrink floors are all numbers taken from ONE face. **Two fonts means every one of
those measurements is now per-font**, and the failures are the silent kind: `MenuKit.Label`
overflows rather than wrapping, and `verticalOverflow = Truncate` drops a whole line without a
warning. Render every screen either side, and `AspectRatioProbes` at all nine shapes, before
calling any of it done.


### 133.4 ⚠️⚠️ THE SCOPE, AND IT IS A HARD LINE

🧑 2026-09-03, setting it: *"tell it that it cant edit the game ui yet"*, *"i need to see lobby
settings and char select as well as loging"*, *"basically everything not game, in this new ui
first"*.

⚠️⚠️ **IF IT IS DRAWN WHILE A ROUND IS LIVE, IT IS OUT OF SCOPE.** Not deferred, not "do it last":
**do not touch it in this pass.** `Hud`, `AbilityDeckHud`, `AbilityInspectPanel`, `StatusStack`,
`HudDeclutter`, `OffscreenIndicators`, `PlayerNameplate`, `RoleSwapCard`, `EmoteWheel`,
`PausePanel` and `ComicPopup` are the in-match layer and they stay exactly as they are.

⚠️ **AND THAT IS A GOOD BOUNDARY RATHER THAN AN ARBITRARY ONE.** The HUD answers to
`docs/VISION.md` § 3, which is a different contract from the front end's: *"the in-match HUD
carries no sentences"*, the deck is a glyph and a key and a ready state, and the three layers have
to stay in step with each other. Repainting it in the same pass as the lobby would put a font
change, a palette change and a readability contract in one commit with no way to tell which broke
what. `PaperPurityProbe`'s own header already scopes the HUD out for the same reason.

**THE FOUR HE WANTS TO SEE, AND THIS IS THE ORDER HE NAMED THEM IN:**

| Screen | Where it lives |
|---|---|
| **The lobby** | `LobbyChrome`, `LobbyCast`, `LobbyJoinPanel`, `LobbyNameplates`, `LobbyChat`, `YouCard`, `ConvertedMatchSetup` |
| **Settings** | `ConvertedSettingsPanel`, and `UiRows` is what it is built out of |
| **Character select** | `ConvertedCharacterSelect`, including the loadout board § 132 just rebuilt |
| **Login** | `SignInScreen` |

Everything else in the front end follows: `ConvertedMainMenu`, `ConvertedModeSelect`,
`ConvertedMultiplayerSetup`, `CustomGameScreen`, `CustomCharacterScreen`, `PlayerHub`,
`ConvertedMatchResult`, `ConvertedCreditsPanel`, `SplashScreen`, `QueueCard`.

### 133.5 ⚠️⚠️ NO LEFTOVER UI, AND THERE IS ALREADY A GATE FOR IT

🧑 2026-09-03: *"ask it to be wary of having leftover ui shit"*. ⚠️ **HE HAS SAID THIS BEFORE, IN
CAPITALS, ABOUT THE LAST OVERHAUL**, and `PaperPurityProbe` exists because of it:

> **"ALSO BE AWARE THAT UR OVERHAULING THE UI, MAKE SURE U COMPLETELY REPLACE UI BCZ I DOTN WANT
> LEFTOVER SHIT FROM OLD UI TO STILL BE FRIGGING WITH US"**, and *"MAKE SURE EVERYTHING U REPLACED
> IS ACCOUNTED FOR AND WE DONT LOSE BUTTONS"*.

**Those are two different worries and that probe answers both. Use it, extend it, and do not write
a second one.** § 119.6 is its entry and its header lists the four things a screenshot cannot see:
a surface inside a shut drawer, a surface underneath another surface (`SkinLayers` leaves a `Face`
and a `Shadow` child behind), a `GodotButton` that only writes its sprite on HOVER so it is correct
in every screenshot and flips to wood the moment a pointer touches it, and a state the shot pass
did not happen to open. **§ 117.7 is seven faults that every probe in this repository was green
through.**

⚠️ **THE INVENTORY IS § 119.3 AND NOTHING ON IT MAY DISAPPEAR.** `EveryLobbyControlSurvived`
asserts each named control still resolves, is reachable and has a handler. That list covers the
lobby; **the same list has to be written for settings, character select and login before they are
rebuilt, not after**, or "we lost a button" is a thing somebody notices in a build.

### 133.6 THE BRIEF ITSELF: BETTER, NOT REARRANGED

🧑: *"ask it to overhaul the entire ui and think of better way to do visual hierarchy"*, and
*"it can use current as inspiration but i want better one"*.

⚠️⚠️ **"USE IT AS INSPIRATION" IS PERMISSION TO CHANGE THE COMPOSITION, NOT TO IGNORE WHAT WAS
LEARNED.** The screens are the result of four rejections and every one of them is a receipt, not a
preference: § 92 *"theres liek 20 shits at once"*, § 94.7 *"its so messy and ugly"*, § 100 the art
cut off, § 121.1 the wooden primary standing in a row of paper. **A new layout that re-earns any of
those is worse than the one it replaced however fresh it looks.**

**So the hierarchy question is the actual work, and `CLAUDE.md` § 6.2c is where it is answered per
rectangle.** For every screen, answer these before drawing it and again before calling it done:

1. **What is the ONE thing on this screen?** Everything else is sized, placed and coloured against
   it. Two things competing means one of them is decoration. ⚠️ § 132.6 is this exact fault found
   at the size of a single card: a name and its own name again, same colour, same weight.
2. **What is the first press, and can the player guess it?**
3. **What is on screen that they do not need RIGHT NOW?**
4. **How do they get out, and is it one press?**

⚠️ **AND `FUTURE.md` § 0.5b IS THE METHOD, NOT THIS LIST.** Five questions before writing a screen,
four ordering tools **in order** (position, size, weight and colour, space), and a table of what
actually transfers from the games it copies. **A new body font makes "weight" a real tool for the
first time**, because until now every bold in this front end was a smear (§ 132.8), so the ordering
tools were three rather than four.

### 133.7 THE FEELING, IN HIS WORDS, AND IT IS TWO THINGS AT ONCE

🧑 2026-09-03: *"i want it so that shit isnt overwhelming and that the game is easy to look at"*,
and *"i want it to feel quirky like the work in progress logo"*.

⚠️⚠️ **THOSE PULL AGAINST EACH OTHER AND THE WHOLE JOB IS HOLDING BOTH.** It is `VISION.md` § 2's
argument moved from the arena to the front end: *"more stuff" is the point and "unreadable" is its
failure mode*. A quirky screen and a calm screen are not opposites, but a screen that answers
"quirky" by adding things is exactly the one that gets rejected, and this project has the receipts:
§ 92 *"theres liek 20 shits at once"*, six buttons in six visual languages.

**Where the quirk goes, taken from the logo itself:** it is a hand-drawn wordmark with a thick
uneven outline, letters that lean, a blob behind them and a drip running off the corner. **The
personality is in the SHAPE and the LINE, not in the count.** That is the same answer § 6.5 already
reached from the other direction: *"a chamfer means pressable and a round means furniture"*, and
*"the issue with old UI is everything feels repetitive bcz i think u use the same code to generate
them all"*.

**So, concretely, and each of these adds character without adding a single thing to read:**

- **The outline is the motif.** The logo's identity is a heavy irregular stroke. A front end whose
  surfaces share that stroke is quirky before a single decoration is added.
- **Let things sit slightly off-square.** A card that leans a degree reads as drawn rather than
  generated. ⚠️ **Not the text.** Rotated type is unreadable type and `AspectRatioProbes` measures
  what a label needs, not what it looks like.
- **Spend the personality on the ONE thing per screen**, which is § 6.2c question 1 and § 133.6.
  The primary action can be as loud as the wordmark. The nine rows under it cannot.
- ⚠️ **AND THE QUIET HALF IS THE NEW BODY FONT DOING ITS JOB.** § 133 exists because one display
  face is setting four-line ability descriptions. **Darumadrop carries the quirk, the body face
  carries the reading**, and that split is what lets a screen be both at once. It is the single
  biggest reason this pass is worth doing.

⚠️ **THE TEST FOR ADDING ANYTHING IS WHAT THE PLAYER HAS TO HOLD IN THEIR HEAD**, not what it costs
to build. `CLAUDE.md` § 6.2, and 🧑's own: *"the cutting shit i want should be focused onn things
that overcomplicate game for ppl"*.

### 133.8 ⚠️⚠️ AND IT HAS TO BE FINDABLE WITHOUT BEING TAUGHT

🧑 2026-09-03: *"i want it to feel intuitive to use as well, in a way that a user would be able to
find everything on their own bcz these controls are familliar to them already"*.

⚠️⚠️ **THE SECOND HALF OF THAT SENTENCE IS THE INSTRUCTION AND IT IS NOT THE SAME AS "MAKE IT
CLEAR".** He is asking the new UI to spend other games' teaching: a control shaped like one the
player has already used somewhere else needs no discovery at all. **Invention is the expensive
option, and this front end has paid for it twice.**

⚠️ **HE HAS ASKED FOR THIS THREE TIMES NOW AND `CLAUDE.md` § 6.2 AND § 6.3 ARE BOTH ALREADY HIS
WORDS**: *"i want the user experience of movinng around the game to feel intuitive"*, *"i wwant the
user experience for the UI of this app to feel intuitive and easy to navigate and not
overwhelming"*, and *"lets say im a player and i want to do something or find something, make sure
that entire experience feels great"*. **A fourth statement of it means the first three were not
delivered**, and § 96 is the receipt with his name on it: he commissioned the player hub and then
could not find the way into it, while `PlayerHubLayoutProbe` was green at all nine resolutions the
whole time.

**What that asks of this pass, concretely:**

- ⚠️⚠️ **WALK EVERY JOURNEY OUT LOUD BEFORE BUILDING ANY OF IT**, which is § 6.3: *"I want to X"*
  to *"X is done"*, naming every press. **More than three presses, or one press that has to be
  discovered rather than read, and the flow is the bug.** Do this for at least: change my
  character, change my loadout, host a game with my rules, join my friend's code, find my profile,
  change my keybinds, sign out.
- ⚠️ **EVERY DESTINATION HAS A VISIBLE DOOR, AND A DOOR IS A THING THAT LOOKS PRESSABLE.** § 96's
  hub had exactly one, a corner chip reading a name and a level, which read as a status readout.
  ⚠️ **AND NEVER ADD A SECOND DOOR TO FIX A FINDABILITY PROBLEM** — that is exactly how § 92's
  six-button panel happened. **Fix the door or move it.**
- **Borrow the layout players already know for each job.** § 118.3 and `FUTURE.md` § 0.5b already
  carry a table of what transfers from the games he named by name, and it is worth more here than
  anywhere else in the project. A settings screen that looks like a settings screen is not
  unimaginative, it is finished.
- **Escape backs out on every screen, always, innermost layer first.** `ConvertedScreen
  .CancelTarget` exists because three screens shipped with a dead Escape. **A player who learns
  Escape is reliable and then meets one screen where it is not has learned that it is unreliable.**
- ⚠️ **A control that does something reacts to the pointer; one that does nothing must not look
  pressable.** And a dead end is a bug: a button that dismisses to nothing is worse than no button.

⚠️⚠️ **THE ONE THING NO PROBE IN THIS REPOSITORY CAN ANSWER IS WHETHER HE FINDS IT.**
`UiClickProbe.EveryButtonIsReachable` has caught new chrome covering a screen three times and it
**cannot** tell anybody that a door nobody looks at is a door nobody finds. `Attention.md` § 5.1 is
the standing ask: launch it, and without being told where it is, get to your profile. **Ship the
pass with that question queued rather than answered**, and do not read a green probe as an answer
to it.

⚠️ **AND `CLAUDE.md` § 4a STILL APPLIES TO EVERY SCREEN THIS PASS TOUCHES.** Three devices, every
time. Build through `MenuKit` or `ConvertedScreen` and the focus path and the thumb targets come
for free; reach for a bare `Canvas` and you have shipped a screen a pad and a thumb cannot use.
`InputSurfaceCheck` refuses the build either way, which is the point of it.

### 133.9 What he added while the pass was running, 2026-09-03

Six more sentences arrived during the session, and **five of them are constraints rather than
preferences**. They are recorded here because § 133.4 to § 133.8 were written before any of them.

| His words | What it settles |
|---|---|
| *"i dont want UI to feel repetitive but we can repeat shit ... js figure out where and what will llook good"* | Repetition is ALLOWED and the placement is the designer's call. `Front_End_Design.md` § 0 answers it: **repeat the chrome, never the composition**, and the test is whether a player has to LEARN the thing. |
| *"i want our ui for lobby and everything (except for game for now) to all have its own identity"*, then *"i want settings, character select profile to all be under the same theme but feel like their own screens"* | ⚠️⚠️ **PROFILE IS NAMED EXPLICITLY NOW**, where § 133.4 had it in the "everything else follows" list. Five screens each get an anchor colour, a borrowed archetype, a hero element and one motif; `Front_End_Design.md` § 2 is the table. |
| *"i want it to be quirky and feel filipino-esque ... but dont force the filipino shit, i js want it to be felt from it"* | ⚠️ **FELT, NOT THEMED.** It is spent on the things a room is MADE of rather than on decoration: the lobby's top band is a tarpaulin strung between two points, so it sags. Nothing is labelled as Filipino and no ornament is added. |
| *"thoroughly make it better plss, i want it to stray away from old design bzc old design was ugly"* | § 133.6's *"better, not rearranged"* restated harder. The composition may change; what was LEARNED in §§ 92, 94.7, 100 and 121.1 may not be re-earned. |
| *"it should have all the functions of old ui, make sure ntohing in old ui as functions get lost"* | § 133.5's second worry, restated. ✅ **Answered by construction**, see § 133.10. |
| *"u figure out as well what secondary font to use ... use font principles or some shit to do this well"*, and later *"u can also use capital and shit depending on stuff, u figure out where all capital looks best and where it doesnt"* | ⚠️ **THE FONT CALL IS DELEGATED**, which closes half of `Attention.md` § 12. The caps question is answered with a measurement rather than a taste, in `Front_End_Design.md` § 3.1. |

### 133.10 ✅ What landed 2026-09-03, and the two numbers worth keeping

**1. The body face is Nunito, and it was chosen by measurement.** Four candidates were rendered at
the sizes the game actually draws at, including the 4:3 1024x768 worst case where one canvas unit
is 0.533 physical pixels. `Assets/TumbangPreso/Art/ui/fonts/SOURCES.txt` carries the table and the
licences; `scratchpad/fontsrc/` carries the scripts and the specimen sheets.

⚠️⚠️ **THE MEASUREMENT THAT DECIDED IT WAS X-HEIGHT, NOT TASTE.** Every layout number in this
repository was taken against Darumadrop's metrics, and what a label LOOKS like at a given
`fontSize` is driven by x-height rather than by em size. Nunito's is **499 per mille against
Darumadrop's 510, a difference of 2.2 per cent**, so moving a row to the body face does not
silently shrink it. Baloo 2 would have cost **6.9 per cent** on every caption in the game, under a
floor § 121.8 is already an open argument about. Nunito's Bold is a drawn weight, 56 per cent more
stem than its Regular, against Darumadrop's none at all. It is 125 KB a weight against M PLUS
Rounded 1c's 3.4 MB, which is 8,201 glyphs for a game set in Latin.

**2. ⚠️⚠️ ALL-CAPS COSTS 10 TO 17 PER CENT OF WIDTH AND LOWERCASE COSTS 2, AND THAT IS § 133.3'S
SILENT FAILURE WITH A NUMBER ON IT.** Darumadrop's caps are unusually narrow against its own
lowercase (x-height over cap height is **0.833**, where a text face is nearer 0.70), so the same
string set in Nunito grows **only in capitals**: `Master volume` +1.9%, `MASTER VOLUME` +12.1%,
`PRESS START TO HOST A GAME` +14.4%, a 60-character sentence +2.8%. `MenuKit.Label` overflows
rather than wrapping, so a capitalised body row that grew simply draws over its neighbour.
`scratchpad/fontsrc/widths.py` re-runs it. **The design answer and the safety answer turned out to
be the same answer**, and `Front_End_Design.md` § 3.1 is the rule: capitals stay where Darumadrop
draws them, plus letterspaced eyebrows and buttons of 12 characters or fewer.

**3. The face split is decided by the type STEP, not by the caller.** `PaperKit.FaceFor` returns
Display at or above `Title` (26) and Body below it, and `PaperKit.Ink`, `Marker`, `Chip` and
`PaperDress.Type` all route through it. ⚠️ **`PaperDress.Type` is worth as much as every hand edit
put together**: settings, character select and match setup are `.tscn` conversions whose labels
have no C# call site, and every one of them passes through that one function when the screen is
papered.

**4. ⚠️ THE SYNTHETIC BOLD IS UNREACHABLE RATHER THAN MERELY REMOVED.** `MenuKit.Apply` swaps the
bold FILE and always clears `fontStyle`, and bold on the display face is a documented no-op
because Darumadrop has nothing to reach for. **28 sites swept** across the nine front-end files;
the 10 in `Hud`, `AbilityInspectPanel`, `ComicPopup`, `EmoteWheel` and `GuidedTraining` are
deliberately untouched, because § 133.4 scopes the in-match layer out.
`PaperPurityProbe.NoLabelFakesItsWeight` is the gate and it asks about the FONT rather than about
the source, so it catches a converted `.tscn` that a grep never would.

**5. ✅ THE CONTROL INVENTORY IS CAPTURED, NOT TYPED, AND IT IS 338 CONTROLS.**
`PaperPurityProbe.NothingOnTheInventoryDisappeared` walks all five screens, every settings tab,
every lobby drawer and all three login states, and writes
`Assets/TumbangPreso/Tests/PlayMode/control-inventory-baseline.txt`: **lobby 70, character 75,
profile 79, login 62, settings 52.** ⚠️⚠️ **§ 133.5 ASKED FOR THIS LIST BEFORE THE SCREENS ARE
REBUILT AND IT WAS TAKEN BEFORE THEY WERE.** A hand-written list of a 40-row settings screen would
have been wrong on the day it was written; a captured one cannot miss a control because nobody
noticed it. Additions pass and removals fail, and a control that kept its node and changed its
lettering is reported as a rename rather than as a loss.

**6. ⚠️⚠️ EVERY TEXT FIELD IN THE GAME HIGHLIGHTED SELECTED TEXT IN BLUE, AND HAD SINCE THE FIRST
ONE WAS BUILT.** `InputField.selectionColor` defaults to `a8ceff`, which is **87 levels more blue
than red**, and `grep -rn selectionColor` over the whole repository returned **nothing**: no site
had ever assigned it. `CLAUDE.md` § 6.4's own test is *"if a hex has more blue in it than red, it
does not belong in a menu"*, and he has had to state that rule six times.

⚠️⚠️ **IT IS INVISIBLE TO EVERY OTHER GATE HERE, WHICH IS WHY IT SURVIVED.** A selection highlight
only exists while text is selected, so it is in no render, no layout probe and no screenshot
review. **It was found in a render by accident** (`Settings-v76.png` happened to catch the username
row with its text selected) and then confirmed by grep, which is § 6.4's own instruction:
*"check it by grepping, not by looking"*. `MenuKit.Dress` is the fix, called at the four places the
game builds a field in code and from both halves of the paper conversion;
`PaperPurityProbe.NoFieldHighlightsInBlue` is the gate, and **it immediately caught a fifth field
the four code sites could not reach**, the settings panel's converted `PlayerNameField`. That is
§ 120.4's lesson one component across: a thing set outside the components the conversion knows
about is a thing the conversion is blind to.

**7. ✅ THE PALETTE IS IN, AND IT WAS READ RATHER THAN TYPED.** He dropped four files in
`~/Downloads/claude/` on 2026-09-03: the colour logo, a mono wordmark, a textured mono wordmark and
a tsinelas-with-a-hit mark. All four are committed unchanged under
`Assets/TumbangPreso/Art/ui/brand/source/`. `tools/read_brand_palette.py` clusters the flat fills,
and it **agreed with itself across two independently drawn files**, which is why these are trusted
rather than sampled: deep red `#980715` (34.3 per cent of the logo), Honey Quartz `#FCD39F` (23.1),
Chartreuse `#D6CE01` (17.0), Persimmon `#FD8041` (5.7), golden `#F5B521` (4.2), rim red `#C32E0D`
(3.8), Army `#B3A828` (1.4). ⚠️ **It reads the ARTWORK rather than the printed swatch labels**,
because four-pixel-tall type through OCR is a guess wearing a lab coat, and the drawing is flat
filled so a histogram returns the fills exactly. ⚠️ **It merges values within 14 levels first**:
the files are JPEG, and its first pass reported the outline as EIGHT colours.

⚠️ **KHAKI IS THE ONE COLOUR THAT IS DERIVED RATHER THAN MEASURED**, because the drawing never
needed a quiet mid-tone. It is Honey Quartz mixed 72:28 toward Army, `#E8C77E`, and
`Attention.md` § 12.1 carries the ask to confirm it against his swatch strip. **Nothing is blocked
on it**: it is one named constant and no surface inlines the hex.

**8. ⚠️⚠️ `UiTheme.Amber` WAS CHANGED TO THE LOGO'S GOLD AND THEN CHANGED BACK, ON PURPOSE.** It is
read **15 times in `Hud.cs`** and again in `AbilityInspectPanel`, `PlayerNameplate` and
`RoleSwapCard`, and § 133.4 scopes the in-match layer out in as many words. **A one-line palette
edit would have repainted the HUD**, which is the same class of accident § 6.4 records
`UiTheme.Ink` causing in the other direction: one constant reaching further than the person
editing it realised. The front end uses `BrandGolden`; the two live side by side until the HUD is
repainted deliberately.

**9. The paper ramp is one colour at four tints, and the ink is measured.** `Paper` `#FEEBD4`,
`PaperWarm` `#FDDFBA`, `PaperEdge` `#FCD39F` and `PaperSunk` `#DEBA8C` are Honey Quartz at four
tints, which is § 6.5's *"one base colour generates a whole control"* moved up a level. ⚠️ **The
ink is a MIX of the two darkest brand colours rather than pure red**, because red text means
"something is wrong" in every convention a player owns: `#55290F` measures **10.5:1** on the page
and `#97491B` measures **5.5:1**, against the old ink's 10.4 and 5.2, so both land on the targets
the old palette was argued to rather than near them. `scratchpad/fontsrc/ramp.py` computes them.

**10. `tools/build_brand_art.py` keys the page to alpha and recolours the wordmark per screen.**
The masters arrive as JPEG on a white page and both halves are fatal in a UI: an opaque white
rectangle behind a logo is a white rectangle, and JPEG rings every hard edge, so a single-threshold
key leaves a grey halo. It keys on LUMINANCE so the ringing goes with the page, trims to the
drawing (§ 6.2c: *"is this image fitted to the region it is SEEN in"*), and paints the mono master
per screen. ⚠️ **That is § 6.5's own mechanism one level up**: *"`JOIN BUTTON.png` is `BUTTON
LONG.png` with one colour swapped, so one base colour generates a whole control"*. He asked for it
by name: *"u can edit those assets and change the colors or smth, depending on which screen u will
use"*.

**11. ⚠️⚠️ `AspectRatioProbes` NOW REPORTS EVERY FAILING LABEL INSTEAD OF THROWING ON THE FIRST,
AND THAT ALONE CHANGED WHAT THE PROBE IS SAYING.** `Assert` inside a walk stops at the first
failure, so the probe named ONE label however many were red, and § 130.15 recorded that one as
*"the only remaining red"*. **It is five.** With the words in the report as well as the node name,
which matters because `MenuKit.Label` calls every label it makes `Label`:

| The label | Size | What it is |
|---|---|---|
| `"Q"`, `"E"`, `"F"` | **13** | The ability KEYCAPS on the loadout board, three of them |
| `"build a character"`, `"your two skills"` | **16** | The two door captions, which ARE `PaperKit.Caption` and § 121.8's open question |

⚠️ **THE KEYCAPS ARE FIXED AND THE CAPTIONS ARE NOT, AND THAT SPLIT IS THE POINT.** A keycap is
pure instruction: `docs/VISION.md` § 3 says *"a screen that teaches the wrong key is worse than one
that teaches none"*, and at 13 units it is **8.7 physical pixels at 720p**, which teaches none. It
is also ONE CHARACTER, so § 126.13's three options collapse to one: there are no words to cut and
no exemption to argue, so **the box grew**. The chip goes 26x18 to 34x26, which matches the ability
glyph beside it exactly so the row's height does not move, and the label goes to
`MenuKit.MinReadableUnits`. **The two captions stay red on purpose**: they are 🧑's call, and
`Attention.md` § 12.2 asks for it against the new face.

**12. The login screen draws the NEW wordmark, and the slot is authored by HEIGHT now.**
⚠️⚠️ **WHICH DIMENSION IS AUTHORED WAS THE ACTUAL BUG WAITING TO HAPPEN.** `BuildLogo` sized the
mark by WIDTH for its whole life, so the height of the game's name depended on the aspect of
whatever file was loaded, and the column below it is a stack. The old `TUMP.png` is **3.48:1** and
the new mark is **1.52:1**, because the new one carries the blob and the drip as well as the
letters: **at the old 336-unit width it would have drawn 221 units tall against 96.5 and landed on
top of the tab row.** `LogoMarkHeight` is 150 and the width follows, so a logo of any shape can
never push the form down again. ⚠️ **And the aspect is read off the texture** rather than written
as `1835.0f / 527.0f` with a note calling a re-export "a one-line correction": that is § 5's drift
in miniature, and he sent two versions of this logo in one day.

⚠️ **`LogoInset`, `LogoPlaqueWidth` and `LogoPlaqueHeight` are deleted and were already dead.**
The plaque went when 🧑 asked *"this looks very ugly i dont get why tump is in a box"*; the three
constants that sized it survived, computed and read by nothing, and `BuildLogo`'s comment still
claimed they *"are kept and still size the slot"*.

**13. ⚠️⚠️ THE MONO MASTER IS A SINGLE-FILL DRAWING, SO A RECOLOUR ONLY WORKS ON A CONTRASTING
GROUND, AND THE FIRST RENDER OF THE LOGIN SCREEN FOUND IT.** `new tump text.jpg` draws the
letters, the blob behind them and the drip as ONE white counter, so any recolour paints all three
the same colour and the shapes collapse into one silhouette. `tump_wordmark_login.png` is deep-red
line on Honey Quartz fill and the login column IS Honey Quartz: on `SignInBoot-v77.png` **only the
outline read**, and the game's name arrived as an empty wire frame.

⚠️ **So the variants are right where the ground is far from the fill and wrong where it is the
fill.** `tump_wordmark_stage.png` is honey on Army and reads well, which is the character screen.
**The login screen draws the COLOUR master instead**, which is also the more faithful answer:
`docs/VISION.md` § 6 and `CLAUDE.md` § 6.4 say his art is the design system and is not to be
repainted, and `tump_logo.png` is the file he actually drew, drawn at `Color.white` on the ground
it was drawn against. ⚠️ **A per-region recolour would need the master separated into layers**
(letters, blob, drip) the way `SkinLayers` separates a control, and that is a request to him
rather than something a script can infer from one flat fill.

### 133.12 ⚠️⚠️ THE SECOND BATCH: HE LOOKED AT THE FONT PASS AND ASKED FOR THE ACTUAL OVERHAUL

🧑 2026-09-03, sent the login render from the first batch: **"yea this shti sucks hahaha, can u
overhaul it like all of it gang, i dont wanna use the old colors anymore"**, then *"remake the
colors too"*, *"i want colors corresponding to or using the same colors as my logo"*, and
**"i asked for an overhaul man, not a quick small chanve"**.

⚠️⚠️ **HE WAS RIGHT AND THE DIAGNOSIS IS WORTH KEEPING.** The first batch moved the PAPER onto the
logo's palette and left every ACTION colour where it was, so what he opened was a warm cream screen
with the old bright green primary `21a131`, the old brown tabs and the old amber still on it. **A
palette pass that repaints the ground and not the objects reads as no palette pass at all**, because
the objects are what the eye goes to.

**Five things moved, and only the first was a colour.** The last two came out of the RENDERS
rather than out of a review, which is § 6.2a's whole argument: *a green layout probe is not a
good screen.*

**1. Twelve front-end constants moved onto the logo's palette.** Verified front-end-only first, by
counting how many of the eleven in-match files read each one, because § 133.4 scopes the HUD out:
`MenuGreen` `21a131` to `a09b01`, `MenuGreenLit` to `e8e14a`, `MenuGreenFace` `51dd38` to
**`d6ce01`** (Chartreuse, the action), `MenuRed` `ed2136` to **`980715`**, `MenuRedLit` to
`c32e0d`, `WoodFace` and `WoodPanelFace` `793e1f` to `3f3a0e` (Army darkened), `WoodFieldFace` to
`2a2709`, `WoodSlot` to `242109`, `Asphalt` to `322f0b`, `Panel` to `fddfba`, `Card` to `feebd4`.
⚠️ **`Cream`, `CreamMuted`, `WoodDeep`, `WoodMid`, `WoodDark`, `WoodEdge` and `Highlight` are NOT
in that list** and were left alone: every one of them is read by the in-match layer.

**2. ⚠️⚠️ THE BUTTONS WERE REDRAWN, NOT RETINTED, AND THAT IS THE HALF THAT MATTERED.** 🧑:
**"the darumadrop buttons AS TEXT stay, i wanted u to remake all buttons in a diff style that feels
like my logo bruh"**. Every pressable surface in the front end was a LIT SOLID: a value ramp down
its face, a bright keyline outside a dark rim, a cast shadow under it. That vocabulary is
`WoodCraft`'s, sampled off his own `BUTTON LONG.png`, and it is faithful to that art. **The logo is
drawn by different rules entirely**: no ramp anywhere, no bevel, no lit edge, every shape a flat
colour inside a heavy irregular line, and the only depth in the mark a darker red bar tucked inside
the bottom of each letter. `PaperCraft.Surface.Brand` is that construction, and
`docs/Front_End_Design.md` § 1.4 carries the four measurements.

⚠️ **THE PAINTERS MOVED AND THE SURFACES DID NOT, WHICH IS THE ONLY REASON THIS WAS SAFE.**
`Surface.Action`, `Token` and `Live` are read BY NAME in about eight places to decide label colour,
the tab inversion and which children to silence (`PaperKit.MakeAction`, `PaperButton.Restyle`,
`PaperKit.MarkLive`, `PlayerHub.Highlight`, two guards in `PaperDress`). Repointing the enum would
have moved a look and broken a contract in the same line; swapping which painter each dispatches to
moved exactly what he asked about and touched no contract.

⚠️ **`PaintAction` is kept, unused, with its receipts.** Every number in it was measured off
`BUTTON LONG.png` and tuned against three of his own rejections. **He has reversed a look before by
name** (the character select going back to brown), and rebuilding that painter from its comments
would lose the measurements. Same argument as `GameVersion`'s branch machinery in `CLAUDE.md` § 7.

**3. The face split moved, because the first threshold was too greedy.** 🧑: **"ur over replacing
fonts, i lowk js wanted u to replace sub fonts with the new font, not everything gang"**, and
*"i think everything here in darumadrop looked good, just change your username to the sub font"*.
`PaperKit.FaceFor`'s boundary went from `Title` (26) to `Body` (20), so **Nunito is the SUB font**:
captions, hints, placeholders, the quiet second line and the ability descriptions, and nothing
else. ⚠️ **The fault § 133 exists for is still fixed**, because the smear was
`FontStyle.Bold` on a one-weight face and `MenuKit.Apply` makes that unreachable on both sides.

⚠️ **AND ONE LABEL HAD TO BE FIXED BY HAND, WHICH IS THE LESSON.** The login screen's USERNAME and
PASSWORD captions are built through `MenuKit.Label` rather than `PaperKit.Ink`, so `FaceFor` never
ran on them and they stayed in the display face while every other caption moved. **A label that
bypasses the kit bypasses the rule**, which is the same class of miss as the converted `InputField`
that kept Unity's blue selection highlight.

**4. Two contrast defects came out of the RENDERS rather than out of a review, and both are the
same shape.** Cream lettering on Chartreuse measures **1.2:1** where ink on it measures **9.1:1**,
so the screen's one primary was in a colour nobody could read. The cause was `PaperButton._darkFace`
asking the SURFACE: for the whole life of that property an `Action`'s fill was a dark slab, so the
surface alone answered the question, and under `Surface.Brand` **both of its fills are light**. It
asks the accent now. ⚠️ **That is `CLAUDE.md` § 6.4's lesson on the other axis**: a colour that was
correct against the surface it was chosen for is not a colour, it is a PAIRING, and moving the
surface without moving its partner is how a label goes quietly unreadable.

The second was the lobby still requesting `Accent.Wood` for START MATCH, which he chose by name
when it meant brown and which now means Honey Quartz: **the screen's one primary was drawing honey
on a honey rail with no presence at all.** It is Chartreuse, which is § 4's role table.

**5. And the last brown objects on the lobby were the ROOM CODE plate and the seat tags.** They are
`PaperCraft.Surface.Sign`, which painted from `WoodMid` over `WoodDeep`: two of the seven constants
this pass deliberately did NOT move because the in-match layer reads them. ⚠️ **The pointer moved
rather than the constant**, which is the same move the painters made one level up: `Sign` reads
`WoodFace` and `WoodSlot` now, cream on it still measures about 9:1, and the HUD is untouched.

### 133.13 ⚠️⚠️ REJECTED. THE 2026-09-03 PASS WAS A RE-SKIN AND HE SENT IT BACK

🧑, having opened the finished batch: **"that shit sucks u didnt overhaul the ui u js recolored it
FUCK i asked for a complete overhaul bcz the current ui was ugly"**, then **"the ui u made was ugly
too btw"**, and again **"the recolor u did was ugly btw"**.

⚠️⚠️ **HE IS RIGHT, AND HOW IT HAPPENED IS THE MOST USEFUL THING IN THIS ENTRY.** The pass
delivered a second font, the logo's palette and a new button material. **It did not move a single
layout.** All five screens have the composition they had before, rectangle for rectangle. Worse:
`docs/Front_End_Design.md` § 2 SPECIFIES all five compositions, § 133.11 says in as many words that
they *"are the work that is left"*, and the session wrote both, built the system instead, and then
reported as though the job were substantially done. **The design was written and not built, and the
report did not say so loudly enough to be heard.**

⚠️ **DO NOT TREAT `Logs/shots-runtime/*-v83.png` AS A TARGET.** They are the rejected state. The
palette, the two faces and `PaperCraft.Surface.Brand` may stay; **the arrangement of every screen
is the job.** § 118.1's eight faults are all still true, including row 2, measured: **680 units of
dead space down the lobby's left side and 475 down its right.** No palette fixes that.

⚠️⚠️ **AND THE INSTRUCTION HE ADDED WHEN HE ASKED FOR THIS HANDOFF IS THE ONE TO ACTUALLY OBEY:
"ask it to genuinely THINK abt how to make it good and capture the quirkiness and style of the
logo."**

**That is a design problem, not a checklist, and the failed pass is what following a checklist
looks like.** It answered every instruction he gave, one at a time, in the order he gave them, and
produced a screen nobody would call good. **Sit with the mark before writing any code.** It is a
hand-drawn wordmark with letters that lean at different angles, a stroke whose weight varies along
its own length, a chartreuse blob shoved BEHIND the letters and overlapping them, an orange fill
that runs diagonally across one glyph and stops, a drip escaping the bottom-right corner and
ending in a swirl, and darker red bars tucked under the letters like a shadow somebody drew by
hand rather than computed.

**None of that is a rounded rectangle in a row with nine of its siblings.** The character is in
things OVERLAPPING, things sitting at angles to each other, one element escaping its own boundary,
and a silhouette that is irregular at the outside edge rather than at the corner radius. **A front
end that captures the logo is one where objects overlap, lean, and break out of their rails** the
way the blob and the drip do, not one where every object is the same pill in the same grid with a
red line around it. **The failed pass drew the red line and kept the grid.**

⚠️ **AND THE COUNTERWEIGHT IS ALREADY WRITTEN AND STILL BINDING**, so this is not permission to
add things: § 133.7's *"the personality is in the SHAPE and the LINE, not in the count"*, § 92's
*"theres liek 20 shits at once"*, and `CLAUDE.md` § 6.2's *never overwhelming*. **Quirk comes from
how the existing elements are arranged and shaped, not from new elements.**

### 133.14 EVERYTHING HE ASKED FOR, IN HIS OWN WORDS, IN ONE PLACE

⚠️ **The nine in § 133.9 are the first half; these are the rest, from the same day, and several
SUPERSEDE earlier instructions.** They are collected here because the pass that failed had them
scattered across a conversation and answered them one at a time.

| What he said | What it settles |
|---|---|
| **"i asked for an overhaul man, not a quick small chanve"** | The size of the ask. Not a pass over the existing screens. |
| **"can u overhaul it like all of it gang"**, *"i dont wanna use the old colors anymore"* | All of it, and no old colours anywhere in the front end. |
| *"remake the colors too"*, **"i want colors corresponding to or using the same colors as my logo"** | ✅ Done, § 133.12 point 1. The palette is measured and in `UiTheme`'s brand block. |
| **"i wanted u to remake all buttons in a diff style that feels like my logo bruh"**, *"the darumadrop buttons AS TEXT stay"* | ✅ Done, § 133.12 point 2. `PaperCraft.Surface.Brand`. The LETTERING was never the complaint. |
| *"ur over replacing fonts, i lowk js wanted u to replace sub fonts with the new font, not everything gang"*, *"i think everything here in darumadrop looked good, just change your username to the sub font"* | ✅ Done. Nunito is the SUB font only; `PaperKit.FaceFor`'s boundary is `Body`. |
| *"u can also use capital and shit depending on stuff, u figure out where all capital looks best and where it doesnt"* | Rule written in `Front_End_Design.md` § 3.1, **not yet applied to the call sites**. |
| **"i want it to be quirky and feel filipino-esque ... but dont force the filipino shit, i js want it to be felt from it"** | ⚠️ **NOT BUILT.** Felt, not themed: spend it on what a room is MADE of rather than on ornament. |
| **"i want our ui for lobby and everything (except for game for now) to all have its own identity"**, *"i want settings, character select profile to all be under the same theme but feel like their own screens"* | ⚠️ **NOT BUILT AND IT IS THE HEART OF THE REJECTION.** `Front_End_Design.md` § 2 gives each screen an anchor colour, a borrowed archetype, a hero element and a motif. None of it exists in code. |
| *"i dont want UI to feel repetitive but we can repeat shit ... js figure out where and what will llook good"* | Answered in § 0 of that file: **repeat the chrome, never the composition.** |
| **"maybe we could use like reoccuring shit to guide ppl"**, relaying Paul Andrei: *"maybe pwede natin iincorporate yung crown thingy sa game"* | ⚠️ **NOT BUILT.** `tsinelas_hit.png` is generated and used nowhere. § 1.1: the mark means THIS ONE, at most once per screen. |
| *"u figure out what else we can reuse or make reoccur"* | § 1.2's six signs. ⚠️ Only the outline is built. |
| **"u can add random shit and designs to the ui too btw to give our screens character, not everything has to be functional"** | ⚠️ **NOT BUILT.** § 1.3: decoration is free where nothing has to be read, and the 680 units of dead lobby are exactly where it goes. |
| *"thoroughly make it better plss, i want it to stray away from old design bzc old design was ugly"* | The composition may change. What was LEARNED in §§ 92, 94.7, 100 and 121.1 may not be re-earned. |
| **"it should have all the functions of old ui, make sure ntohing in old ui as functions get lost"** | ✅ Answered by construction: `PaperPurityProbe.NothingOnTheInventoryDisappeared`, 338 controls captured BEFORE the pass. **It is what makes tearing the screens apart safe.** |
| *"check this folder out in downloads u can use these for some shit ... u dont have to use it too, u can js use for inspiration"*, *"u can edit those assets and change the colors or smth, depending on which screen u will use"* | `~/Downloads/claude/`, committed to `Art/ui/brand/source/`. `tools/build_brand_art.py` recolours the mono master per screen. |

⚠️⚠️ **AND THE THREE OLDEST ONES STILL GOVERN, WHICH IS WHY THEY ARE IN `CLAUDE.md` § 6.2 RATHER
THAN HERE:** *not overwhelming*, *easy to look at*, and **findable without being taught**. § 96 is
the receipt with his name on it and the door into the hub is still not fixed.

### 133.15 ⚠⚠ THE SECOND ATTEMPT AT § 133.13, 2026-09-03, branch `abilities-rework`

🧑 opened the handoff with the instruction that governs this whole entry: **"ask it to
genuinely THINK abt how to make it good and capture the quirkiness and style of the logo"**,
then, watching it: *"i want u to really think abt composition and visual harmony"*, *"will this
be pleasing, intuitive and not overwhelming? is everything placed right"*, *"be aware of empty
space and shit, as well as negative space and visual hierarchy"*, and **"start building it into
the game but try to give it even more personality , all buttons"**.

⚠⚠ **THE COMPOSITIONS WERE DRAWN BEFORE ANY CODE WAS WRITTEN, AND THAT IS THE ONE
PROCEDURAL CHANGE FROM THE PASS THAT FAILED.** § 133.13's diagnosis is that the last session
*"wrote both, built the system instead, and then reported as though the job were substantially
done"*. `docs/Front_End_Design.md` § 2 specified five compositions in prose and nothing drew
them, so nobody could see that they had not been built. A drawing cannot hide that.

#### What landed

**1. The sub font is Familjen Grotesk, and Nunito is gone.** 🧑: *"i think our sub font sucks
its too ai"*. It is a fair reading and the measurement says why, in
`Assets/TumbangPreso/Art/ui/fonts/SOURCES.txt`: **x-height alone is the wrong test for a
pairing.** It answers "does a row shrink when it moves to the sub face" and says nothing about
whether two faces look related. The measurement that answers that is x-height over CAP height,
and Darumadrop's is extreme at **0.833** where a text face is nearer 0.70. **Nunito's is 0.694,
the lowest of every candidate measured**: it had the closest x-height and the worst proportion
at once, which is what reads as a stock pairing. Familjen Grotesk is **0.769**, less than half
Nunito's distance, keeps apparent size at **-2.0 per cent**, has a real bold at **+51 per cent**
stem, is **58 KB a weight against 122**, and is **the only candidate that draws all fifteen
glyphs this front end asks for** (Nunito was missing the two triangles, the check and the left
arrow, so four marks in the game were falling through to a fallback face nobody chose).
`Logs/ui/font-pairing-v1.png` is the specimen; `scratchpad/fontsrc/measure2.py` and `stems.py`
re-run every column.

⚠ **AND THE FIRST PASS'S STEM COLUMN WAS PARTLY MEASURING A TAIL.** It took the width of a
lowercase `l`, which in several of these faces carries one. Measured on a plain cap `I` every
candidate has a real bold, so weight stopped being the differentiator and proportion became it.

**2. `PaperCraft.PaintBrand` draws buttons with three things it did not have**, which is
*"give the buttons a bit more personality and cuter"* answered where it was asked rather than
per call site. `docs/Front_End_Design.md` § 1.4 has the table and the measurements.

- ⚠⚠ **The stroke's weight varies along its own length**, ± 26 per cent, sampled on y.
  § 133.13 names this in the rejection in as many words. A constant stroke is a BORDER; the
  mark's line reads 19 to 33 px on 300 px letters.
- ⚠⚠ **Four different corner radii**, via a new `PaperCraft.Depth4`. The old `Depth`
  folds the rect with `Mathf.Abs` and therefore **cannot** tell one corner from another, so
  every brand control in the game was symmetric by construction.
- ⚠⚠ **Every control family is drawn by its own hand**, seeded off surface + accent +
  height by `PaperCraft.Hand`. This is *"the issue with old UI is everything feels repetitive
  bcz i think u use the same code to generate them all"* answered at the level it was asked.
  **The seed deliberately excludes the pose**, or a button would change shape under the pointer.
- Plus the under-bar's ends taper (legal on x, because only the middle column of a nine-slice
  stretches), and hover thickens the stroke by a tenth rather than only lifting the fill.

**3. Fifteen drawn avatars**, `tools/build_avatar_art.py`, because 🧑 cropped the identity chip
and said **"like tf is that pic doing there"**. It was a square cut out of the old Godot
character screen: a picture of a user interface, on a user interface. `Front_End_Design.md`
§ 1.5 carries why they are drawn rather than cut out, and it is not laziness: the knockout is
solvable, the FRAMING is not, because the model stands at a different height in every reference
sheet and **a picker is twelve things seen together**. 172 KB for all fifteen and no credit line.

**4. `BrandSwatchProbe`**, a new editor probe that photographs every surface at every pose at 3x
on the real paper ground, nine-slicing by hand. Every recorded button fault in this repository
was a detail at a scale `UiRuntimeShots` cannot show: § 121.1's grey halo was found by sampling
a PNG row, and *"its a circle and a sharp shape at the same time"* was 🧑 zooming into his own
screen.

**5. `docs/Front_End_Design.md` § 2.2b: the lobby is THREE screens and the tarpaulin is the
slot that changes.** 🧑: *"lets say u click ranked wtf would show?"* ⚠⚠ **That question
had no written answer and the code answered it by hiding things.** `Parts.SetMode` switches nine
objects off and on, so each mode was the custom screen with holes in it, and a hole is what
§ 118.1 row 2 measured as *"four corners and a hole"*. The composition now holds and one slot
carries the one fact each mode exists to produce.

**6. THE LOBBY'S COMPOSITION IS REBUILT**, which is the first of the five and the one 🧑
named first. It is not a repaint: every object below moved, changed kind, or stopped existing.

| Was | Is | Why |
|---|---|---|
| A 68-unit cream island bar holding **six pills of one size in one row** | A **196-unit tarpaulin** that sags in the middle, leans, and runs off BOTH screen edges | The single clearest instance of *"the failed pass drew the red line and kept the grid"*. It is also the whole of "filipino-esque" on this screen and none of it is labelled as such: a vinyl tarp over a barangay court is a thing the room is MADE of. `BrandMarks.Tarpaulin` |
| No screen name at all | **LOBBY**, top left, `Display`, with the seat count under it | `Front_End_Design.md` § 1's first spine row: *"the answer to 'where am I'"*. Every other screen had one; this one had three tabs, and a tab says which mode you picked rather than which screen you are on |
| BACK as a fifth pill **competing with the tab row** (§ 118.1 row 5) | BACK hanging directly under the screen's name | Answered by POSITION rather than by size |
| Three tabs in a `HorizontalLayoutGroup` with `childForceExpand` on both axes | Three tags **hung on cords of three different lengths** | A force-expanding layout group is a machine for producing identical objects in a row, which is *"u use the same code to generate them all"* in one component. ⚠️ **Not rotated**: § 1.2 is explicit that the lean is for chrome and never for type |
| `ACCOUNT`, a 200-unit chip stating a level | The **identity chip**: a drawn face, the player's own name at `Title`, the account state under it, a chevron, hanging off the tarp's bottom edge | ⚠️⚠️ **§ 96'S FIX.** A level is a status readout and people read those; a face with your own name on it is a thing people press |
| `SECURE PROGRESS` as a **fifth tab beside three MODE tabs** | The second line on the identity chip | Signing in is not a place you can be. One fewer object to scan |
| The room code on a plaque in the bottom-right column | The room code **on the tarpaulin**, centre | `Front_End_Design.md` § 2.2b: the composition holds between modes and ONE SLOT carries the fact each mode exists to produce. This is the written answer to *"lets say u click ranked wtf would show?"* |
| A 460x96 primary inside a cream bar | A **560x132** primary with the mark's own **impact burst** behind it, lettering at `Display` | *"i want start match button to have genuine emphases and look adn feel good to press"*. The four ordering tools are position, size, weight, colour IN THAT ORDER, and it already had three |
| A flat cream slab across the bottom third holding three cells | **Nothing.** The rail is transparent and its three groups stand on the street | ⚠️ The layout, the fitter and the self-centring all STAY; only the paint is gone. Every child already carries its own opaque surface, so the slab was buying separation from a background the controls separate themselves from |
| The tsinelas mark generated and **used nowhere** | On your own seat, once per screen | Paul Andrei's crown. § 1.1: it means THIS ONE, at most once, and it is not pressable so it can never become § 6.3's second door |
| 680 units of dead ground down one side and 475 down the other | **Chalk**, in both bottom corners, outside every content rect | *"u can add random shit and designs to the ui too btw"*. § 1.3: decoration is free where nothing has to be read |

**7. The login screen's SEAM is drawn.** `Logs/shots-runtime/SignInBoot-v83.png` is a Honey
Quartz column and a piece of key art meeting at a **perfectly straight vertical line down the
middle of the window**. Every rect fitted its box and every colour was in the palette, and the
one edge the player actually looks at was the one edge in the design that no hand drew.
`BrandMarks.ColumnEdge` replaces it with a torn edge, and the drip escapes across it, which is
`Front_End_Design.md` § 2.1's own mechanism: *"the two objects are one drawing rather than a
picture with a box next to it."*

#### ⚠⚠ FOUR FAULTS THE RENDERS FOUND THAT NO TEST COULD, AND ALL FOUR SHIPPED FIRST

**This is the section worth reading.** Every one of these was green in EditMode, drew without a
warning, and was obvious in a picture.

1. ⚠️⚠️ **`Object.Destroy` ON A COMPONENT THAT PAINTS FROM `Update` DOES NOTHING.**
   `PaperKit.Sheet` attaches a `PaperSkin`, and `PaperSkin.Update` calls `Rebuild`, which writes
   `_image.sprite` every frame. `Destroy` is deferred to the end of the frame, so the tarpaulin
   was overwritten by a plain `Sheet` slab one frame after it was set.
   `Logs/shots-runtime/Lobby-v84.png` is a flat cream band with no sag, no stroke and no ties:
   **the old rail with a new height.** A component that paints from `Update` has to be DISABLED,
   not destroyed, and the honest version is not to create it at all.
2. ⚠️⚠️ **`tsinelas_hit.png` IMPORTS AS A TEXTURE, NOT A SPRITE**, so
   `Resources.Load<Sprite>` answers null and the `Image` drew a **filled white rectangle** on the
   player's own nameplate. ⚠️ **This is the third time in this repository** and
   `SignInScreen.BuildLogo` already records the other two, with the conclusion followed here: a
   `.meta` is a file nobody edits by hand and a re-import can reset it, so the CALLER draws a
   `RawImage`.
3. ⚠️⚠️ **A PIVOT IS NOT AN ALIGNMENT.** The room code was placed 46 units below the top of the
   banner with `pivot.y = 0`, so the rect grew UPWARD from that point and 96 units of plaque went
   off the top of the window. `Logs/shots-runtime/Lobby-v85.png` shows the code cut in half.
4. ⚠️ **A RATIO COMPUTED AGAINST A COLOUR IS NOT A RATIO AGAINST THE DRAWN PIXEL.** The chalk was
   tinted to 0.16 alpha off § 1.3's 1.5:1 ceiling, and the sprite's own strokes are already
   feathered below 1, so the Image tint multiplied a soft mark and produced nothing visible at
   all. **Identical to § 117.7's *"a chalk rule at 0.55 alpha is a quarter-strength mark, because
   the tint multiplies the sprite's own"***, found again three months later in a different file.

#### ⚠⚠ What is NOT done, stated plainly because § 133.13 is about a report that was not
plain enough

- **Three of the five compositions are untouched**: `ConvertedSettingsPanel`,
  `ConvertedCharacterSelect` and `PlayerHub` still have the arrangement § 118.1 and § 121 list
  faults against. Settings in particular is still one scrolling wall with no tabs and no
  collapsing, which is § 92.3b's *"grouping without collapsing does not fix a wall, it aligns
  it"*, and `Front_End_Design.md` § 2.4 specifies the ledger it should be.
- **The login screen has its seam and nothing else.** § 2.1's composition (the wordmark as the
  hero overlapping the art) is not built.
- **The avatar PICKER does not exist.** The fifteen faces are drawn, installed and used as a
  stable per-name default; 🧑 asked to be able to CHOOSE one and that half is not built.
- **The hatch is drawn and used nowhere**, which is the same fault the mark had before this
  session. `BrandMarks.Hatch` exists; no disabled control routes through it yet.
- **`Checks.RunAll`, the full PlayMode suite and `AspectRatioProbes` have not been run against
  any of this.** EditMode is 327/327 and `UiRuntimeShots` renders, and neither of those is the
  gate § 126.8 and § 131.6 are about.
- **Nobody has looked at it on his window shape**, which is `CLAUDE.md` § 6.2b's third row and
  the one that has caught a collapsed layout before.

#### ⚠⚠ 133.16 WHAT HE SAID LOOKING AT THE RENDER, AND ALL THREE ARE THE SAME NOTE

🧑 cropped three objects out of `Logs/shots-runtime/Lobby-v89.png` and said **"u js
recolored"** again. ⚠️ **The composition DID move and the before/after proves it**
(`Logs/ui/lobby-before-after.png`), but the three things he cropped are the three things on
that screen that are still the old object, so his reading of those crops is correct.

| What he cropped | What he said | Why he is right |
|---|---|---|
| **The banner's left and middle** | *"i thought ud put shit in that thing at the top too, i didnt expect u would keep it blank"* | ⚠️⚠️ **THE TARP IS 196 UNITS TALL AND ABOUT HALF ITS WIDTH CARRIES NOTHING.** It holds the screen name, BACK, the room code and the identity chip, and between the name and the code there is roughly 500 units of bare honey. That is *"be aware of tightness and empty space as well this looks ugly bcz of big ass empty sopace"* arriving on the object built to answer it. **The fix is not to add controls**: § 1.3 permits DECORATION where nothing has to be read, and a printed tarpaulin is exactly the kind of object that carries a drawing. The seat count, a printed edge, the hung banderitas, the mark faded into the vinyl, the map name: all of it belongs to the tarp rather than to a new row. |
| **The DANTE card and the build row** | (cropped together) | **Two near-twin paper rows, which is § 118.1 row 4 unfixed.** `Front_End_Design.md` § 2.2's own design says the build is a **sunk tray INSIDE the fighter card**, so the two are one object with two zones rather than two rows of the same shape. `BuildFighterColumn` still stacks them as siblings in a vertical layout. |
| **The room code plaque** | (cropped alone) | **It moved and it was not redrawn.** It is still `PaperCraft.Surface.Sign`, a dark rounded plate built for a cream RAIL. On a honey tarpaulin the one fact the screen produces should be printed ON the vinyl the way a room code is printed on a tarp, not nailed to it on a separate plate. |

⚠️ **THE PATTERN ACROSS ALL THREE IS THE SAME AND IT IS WORTH NAMING**: this pass moved
objects and drew new ones, and where it MOVED an object without redrawing it, he can see it
instantly. **Moving a control to a better place does not make it the right control for that
place.**

⚠️⚠️ **AND TWO MORE ON THE SAME CROP, WHICH TAKE THE COUNT TO FIVE.** Cropping the whole
bottom row of `Lobby-v91.png`:

| What he said | What it settles |
|---|---|
| **"only start match looks good here u js reused all buttons"** | ⚠️⚠️ **THE PAINTER CHANGED AND THE ROLES DID NOT.** `PaintBrand` now varies its stroke weight, gives every family its own four corner radii and its own edge, and all of that is TRUE of the chips as well as of the primary. What he is seeing is that **START MATCH is the only control on the row with a JOB that shows**: it is Chartreuse, 560 by 132, with a burst behind it, and MATCH SETTINGS, DANTE, Standard Build, JOIN and CHAT are five Honey Quartz rectangles of four similar sizes. **The variation is in the silhouette and the hierarchy is not**, so the eye reads five of one thing and one of another. `Front_End_Design.md` § 1's four atoms exist for exactly this (`Sheet`, `Tray`, `Chip`, `Row`) and this row uses two of them; § 6.5's *"a chamfer means pressable and a round means furniture"* is the shape difference that is still not being spent. |
| **"join a game and chat being there looks ugly af too"** | ⚠️⚠️ **THEY ARE IN THE SPINE'S SLOT AND THEY ARE THE WRONG TWO CONTROLS FOR IT.** § 1 puts secondary chips *"in a row to the LEFT of the primary"*, and that is where they are, so the placement is following the design. What the design did not ask is whether these two belong on the row at all. **CHAT is a drawer, not an action** (it opens a panel; § 1.2 says a thing that opens something is a `Row` with a chevron), and **JOIN is only meaningful when you are NOT already in a room**, which on this screen you always are. § 2.2b's mode table already drops JOIN in ranked for that reason. Candidates: chat becomes a drip-marked drawer on the room's own edge, and JOIN moves to the banner beside the room code, which is the one place on the screen already about codes. |

⚠️ **NEITHER OF THESE IS A PAINT PROBLEM AND BOTH WILL SURVIVE ANOTHER RECOLOUR**, which is
why they are written here rather than left as a note about button styling.


### 133.11 ⚠️ What is still open, and nothing in it is blocked

- ✅ **THE PALETTE IS NO LONGER A BLOCKER.** It landed 2026-09-03, measured, and the repaint went
  through `UiTheme` alone: **every surface in the front end names a semantic constant rather than a
  hex**, so one file moved all five screens at once. That is § 6.4's own receipt used forwards, one
  constant having put navy on every word in the front end.
- ⚠️⚠️ **THE FIVE COMPOSITIONS ARE THE WORK THAT IS LEFT, AND THEY ARE THE BULK OF § 133.**
  `docs/Front_End_Design.md` § 2 is the design and the code is not written. **What landed is the
  system, not the screens**: the palette, both faces, the recurring marks, the inventory gate and
  the three new probes. The lobby renders in the new palette and the new faces, and it is still
  the composition § 118.1 lists eight faults against.
  The order he named is lobby, settings, character select, login, then profile and the rest, and
  the named work per screen is:
  - **The spine** (`Front_End_Design.md` § 1), which is one piece of code used by all five: the
    screen name top left, BACK under it, **the identity chip TOP RIGHT carrying the player's face**
    (this is § 96's fix and the single most-requested unfixed thing in the repository), the one
    primary bottom right, secondary chips to its left.
  - **The mark** (§ 1.1), Paul Andrei's crown, drawn at most once per screen on the thing that is
    currently chosen. `tsinelas_hit.png` is built and unused so far.
  - **The other five recurring signs** (§ 1.2): the heavy outline meaning pressable, the drip
    meaning more below, the hatch meaning unavailable, the chevron, the lean.
  - **The case pass**: § 3.1's rule is written and not yet applied to the call sites. ⚠️ **It is a
    typography job now rather than an overflow one**: since the face boundary moved to `Body`, the
    only step that changed face is `Caption`, so no button and no settings row grew by a unit. 81
    ALL-CAPS literals of 13 characters or more are in the front end, and the ones that matter are
    the SENTENCES (a 90-character penalty line in `MatchResult`, the empty states in
    `ConvertedMultiplayerSetup` and `LobbyJoinPanel`), not the buttons, which he confirmed by eye:
    *"i think everything here in darumadrop looked good"*.
- ✅ **`CLAUDE.md` § 6.4's palette list is rewritten**, in the same commit as the hexes, with its
  ban and its receipts kept and the carved wood retained and labelled as the OLD palette.
- **§ 121.8**, still 🧑's call, and `Attention.md` § 3 says to answer it against the NEW body face
  rather than before it. A face with a different x-height changes the measurement the question was
  asked against.
- ✅ **`AspectRatioProbes` WAS RUN FOR THE FONT CHANGE, WHICH IS § 133.3'S NAMED TRAP, AND THE
  FONT DID NOT MOVE IT.** It comes back **1 passed, 1 failed**, and the failure was verified as
  pre-existing rather than assumed to be: the tracked changes were stashed, the probe was re-run
  against clean HEAD, and it returned the **byte-identical message**. So two fonts did not break
  a single layout at any of the nine shapes, which is the x-height measurement in § 133.10 point 1
  paying off exactly where it was supposed to.

  ⚠️⚠️ **AND § 130.15 IS NOW STALE, WHICH IS WORTH MORE THAN THE RESULT.** That entry says the
  character screen's *"only remaining red is `DoorCaption`, authored at 16, which is exactly
  `PaperKit.Caption`"*, and points at § 121.8 as the thing holding it open. **The actual first
  failure is a label called `Label` authored at 13**, and it is 13 on clean HEAD too.
  `AspectRatioProbes` asserts inside a loop, so NUnit stops at the FIRST failing label and every
  later one is invisible: the caption at 16 may well still be red behind it, and nobody can tell
  from the report. **`Attention.md` § 3 has therefore been asking 🧑 to settle a question that is
  not what is failing.**

  ✅ **RESOLVED THE SAME SESSION, ONCE THE PROBE WAS MADE TO REPORT ALL FIVE WITH THEIR WORDS.**
  `MenuKit.Label` names every label it makes `Label`, so `'Label' at 13` named the fault without
  naming which of five call sites it was. With the lettering in the report the answer was the
  ability **KEYCAPS**, `"Q"`, `"E"` and `"F"`, at 8.7 physical pixels at 720p on the one label that
  is pure instruction (`docs/VISION.md` § 3: *a screen that teaches the wrong key is worse than one
  that teaches none*). One character, so § 126.13's three options collapsed to one and **the box
  grew**: the chip went 26x18 to 34x26, which matches the ability glyph beside it so the row height
  did not move. **What is left is the two door captions at exactly `PaperKit.Caption`**, which is
  § 121.8 and his to settle, so the probe now says what § 130.15 always claimed it said.
  ⚠️ **Do not lower the floor** (§ 126.13). Either a label earns a written exemption the way
  `MenuKit.Fit` registers one, or they go up to 18; and the probe should collect every failure
  and report them together rather than throwing on the first, or this will happen again.
- **Render every screen either side of the composition work.** The font-and-palette renders are
  taken (`Logs/shots-runtime/*-v76.png`, `UiRuntimeShots` 9/9); the compositions do not exist yet.
---

## 132 · The loadout said nothing about the hero, and a build vanished the moment the match started ⚠️ IN PROGRESS, 2026-09-03, branch `abilities-rework`

🧑 2026-09-03, twice in one session and about two different things: *"thoroughly make the loadout
experience better too, i dont want the ppl to feel like the characters all js do the same shit"*,
and, on the asset pass beside it, *"make sure ur implementation of sfx and vfx and shit is
genuinely good and u didnt js slap shit in"*.

⚠️⚠️ **THE SCREEN WAS NOT THE FAULT AND NEITHER WAS THE SIDEGRADE RULE.** Both had already been
fixed: § 122.5 moved the board onto the fighter picker where he asked for it, § 122.18 gave every
tile its trade line, its glyph and its progress bar, and `HeroLoadoutTests` asserts all
twenty-four rows are budget neutral. `Logs/shots-runtime/CharacterLoadout-v72.png` is a clean,
readable screen. **What it says is the problem.**

### 132.1 ✅ Twelve default readings that said "as tuned"

On that render the equipped tile reads *"The stomp as it is tuned. One heavy shock at the measured
radius"* and the trade line under it reads **`As tuned · As tuned`**. Six heroes, twelve slots, and
**the half of every row that is already equipped carried no fact about the character at all**. A
player opening DANTE and then CHESKA saw the same two words in the same place both times.

⚠️ **A DEFAULT IS ONE OF TWO READINGS OF AN ABILITY, NOT THE ABSENCE OF A READING**, so it owes the
same two facts its alternate owes: what it gives you and what it costs. All twelve are rewritten,
each transcribed from its own kit rather than invented (Dante's 2.2 m and two charges are
`SeismicStompAbility`'s own `telegraphRadius` and `charges`; Cheska's 2.3 m and Phaister's 2.4 m
the same). SEISMIC STOMP now reads *"A 2.2 m shock at your feet that launches whoever is standing
in it"*, `Throws them clear · Two uses, then none`, against LONG TREMOR's `Takes them down · They
stay close`. **That pair is a choice. The old pair was a thing and a variation on the thing.**

⚠️ **THE NUMBERS DID NOT MOVE.** `Gain` and `Cost` are still exactly 0 on every default and
`Phase10Tests.EveryVariantIsBudgetNeutral` still asserts it. Only text changed.

Two new `Core.Tests` cases, 40 ms, so this cannot come back:
`NoDefaultDescribesItselfAsBeingAsTuned` names the exact phrases that shipped, and
`TheTwoReadingsOfEverySlotAreToldApartByTheirWords` fails if two tiles in one slot share a name, a
description or a trade line. ⚠️ The existing `EveryVariantRowFitsTheTileItIsDrawnOn` caught one of
the new labels at 57 characters against its 48-character band on the first run, which is exactly
what that test was written for.

### 132.2 ✅ The ultimate was not on the loadout board at all

The screen is titled `LOADOUT · DANTE` and showed **two of his three powers**. The biggest single
thing that separates two heroes was the one part of the kit the loadout experience never mentioned.

⚠️⚠️ **IT IS A READ-ONLY ROW AND THAT IS NOT A COMPROMISE, IT IS `AbilityVariant.Slot`'s OWN
ARGUMENT PUT ON SCREEN**: *"an ultimate is banked once or twice a match and reading which one an
opponent has is already a skill; two readings of the same ultimate would make the tell unreliable
rather than deeper."* So the row says that, in one line, where a tile would be. **A rule the player
can read is a rule they can play around; a rule that is only an absence reads as a screen that
forgot something.**

It reuses `BuildSlotHead`, so it cannot drift from the two rows above it, and `BuildSlotHead`
prints `ULTIMATE` for slot 0 rather than inventing a third skill number. `BoardHeight` is stated as
`388 + UltimateRowHeight + SlotGap` rather than retyped as a round number.

⚠️ `LoadoutSurfaceProbe` went red on the first run for the right reason: it asserted **exactly two**
slot heads. It asserts **three heads and four tiles** now, and the pair is the assertion: four tiles
says only the two skills carry readings, three heads says the ultimate is still on the screen.
Either alone would let the ultimate row silently grow tiles or silently disappear.

### 132.3 ✅ A build was invisible the moment the match started, which is most of why they felt the same

⚠️⚠️ `docs/VISION.md` § 3 PROMISES THREE LAYERS THAT STAY IN STEP AND ONLY ONE OF THEM WAS SHOWING
THE BUILD.

| Layer | Where | Was |
|---|---|---|
| **Learn** | Character select | ✅ already drew `AbilityVariant.Name` (§ 122.5) |
| **Recall** | Hold the ability-info key | ❌ drew `HeroAbility.Name` and `HeroAbility.Description` |
| **Play** | The status stack | ❌ drew `HeroAbility.Name` |

**A player who equipped ARC LINE held the info key mid-round and read BOLT SPRINT, with the wrong
sentence under it.** The one screen that exists to answer *"what did I bring"* answered with what
everybody brings, and every status row and cooldown row in the HUD named somebody else's power.

`HeroAbility.VariantName`, `VariantSummary`, `EffectiveName` and `EffectiveDescription` are
`VariantCastCue`'s pattern exactly: same writer (`HeroAbilitySystem.ApplyLoadoutToPresentation`),
same null-means-default fallback, same `internal set`. **On a default build every string in the
game is byte-identical to before this existed.**

⚠️ **THE GLYPH DOES NOT MOVE AND MUST NOT.** `VISION.md` § 3 rule 1: the icon says what the power
does to the WORLD, and a sidegrade does not change the job. Two icons for one slot would teach the
player that the icon means the build.

⚠️ **THE DECK TILE NEEDS NO CHANGE AND THAT IS ALSO § 3**: *"the in-match HUD carries no
sentences."* The deck is a glyph, a key and whether it is up, and none of the three is a name.

### 132.4 What was checked and found NOT to be wrong

Recorded so the next reader does not redo it.

- ⚠️ **All twelve alternates do reach the game.** A first pass counted `ctx.GainScale` call sites
  and concluded three of them did nothing; that was wrong. `sean.2.flare` and `zack.2.discharge`
  reach it through `Carrier` and `Slipper`, and `phaister.2.stride` through `ScaleLoadout(aimMax:)`
  into `HeroAbility.AimedRange`. **Do not delete a variant on a grep.**
- ⚠️ **The twelve `sfx_var_*` cues do play**, despite `AudioCueCheck` reporting all twelve DORMANT.
  They are assigned to `HeroAbility.VariantCastCue` and reached through `EffectiveCastCue`, which
  is an indirection the check's text scan cannot see. Its own message says so.
- **The locked alternates are locked on purpose.** `ChallengesEnforced` is true and the counters
  tick (2/8, 1/6 on the v72 render), so this is not § 92.1's *"fifteen rows of 0/0"*: it is a
  Risk of Rain 2 style unlock a player is visibly making progress on, earnable against bots.

### 132.6 ✅ The TAB tray printed every ability's name twice

🧑 2026-09-03: *"make sure that when u click tab the skills are readable and feel good to the eyes
to read and arent messy af"*. ⚠️ **This is the SECOND complaint about this tray.** The first was
2026-08-29, *"tab is unreadable and so much fucked of text overflow and format"*, and the fix for
that one was structural: every label went to `MenuKit.MinReadableUnits`, the name box went to 1.35x
its type so `DEMONIC CARAPACE` stopped drawing through `62s CD · 4s`, and the body stopped
truncating. All of that was right and none of it is being undone.

⚠️⚠️ **WHAT WAS STILL WRONG WAS NOT THE LAYOUT, IT WAS THE WORDS, AND
`Logs/shots-hero/hero_inspect_dante_v1.png` SHOWS IT IN ONE LOOK.** Every card reads:

```
SEISMIC STOMP                 <- 21 pt bold, Dante's green
[SEISMIC STOMP] · 2 CHARGES   <- 18 pt bold, Dante's green, and amber
Slams the ground under you...
```

**The name, then the name again, in the same colour, one line down.** Eighteen cards across six
heroes, thirty-six copies of eighteen strings. That is two headings competing for one eye on every
card in the tray, and it is `CLAUDE.md` § 6.2c's first question with nothing else left to blame:
*what is the ONE thing on this card.*

⚠️⚠️ **THE CAUSE IS `AbilityIcons.LabelFor` AND IT IS `docs/VISION.md` § 3 RULE 1 BEING OPTED OUT
OF.** That rule: *"The icon says what the power does to the WORLD, not what element it is made of.
`AbilityGlyph` is Zone, Wall, Dash, Shield, Burst, Projectile, Phase, Slam, Empower... Two heroes
with completely different fiction share a glyph when they share a job."* The nine generic glyphs
return exactly those job words. **The nineteen bespoke per-hero glyphs returned their own ability
name instead**, so the one line on the card whose job is to say what KIND of power this is said
nothing a player could compare across heroes.

Every bespoke glyph reports its job now. Three of Sean's, Zack's and Phaister's land on
`TSINELAS BUFF`, which is the most useful single fact the tray can give about any of them: all
three change what your NEXT THROW does rather than producing anything of their own. Two of the six
ultimates are `FROM ABOVE`. **Two abilities sharing a word is the design, not a collision**; the
drawn icon stays unique per ability and `HeroPresentationTests` still asserts that.

Three changes, all in the card:

1. **The kind word is the job**, not the name. `AbilityIcons.LabelFor`.
2. **It is `UiTheme.CreamMuted`, not the hero colour.** A caption in the heading's own colour at
   nearly the heading's weight is a second heading. The name is the one thing on the card.
3. **The brackets are gone.** They were carrying the separation the colour failed to, and
   `card.Meta` already opens with a `·`, so the row reads `AREA BURST · 2 CHARGES` as one line of
   two facts rather than a bracketed aside beside a fragment.

⚠️ **AND THE CARD SHOWS THE EQUIPPED READING NOW**, which is § 132.3: `EffectiveName` and
`EffectiveDescription`. A player who equipped ARC LINE and held TAB used to read BOLT SPRINT.

### 132.7 ✅ The capture frame had been photographing a third of the tray

⚠️⚠️ **AND FIXING THE FRAME IS WHAT FOUND THE SECOND FAULT.** `HeroUiProbe.TrayHeight` was 300 px
against a comment reading *"the tray is 1060 x 236"*. That was true when it was written and stopped
being true on 2026-08-29, when the same complaint that produced the type-size pass made the panel
taller. `Logs/shots-hero/hero_inspect_zack_v2.png` at 300 is **three card bottoms and a grey
field**: no header, no glyph tiles, no names, no key chips. Every review of this tray for weeks was
conducted against a picture of its last three lines. `CLAUDE.md` § 6.2b with the camera wrong
instead of the screen: the render exists, it is green, and it is of something else. 620 now, and
deliberately taller than the panel so the next label that grows does not silently walk out of shot.

**What the full frame then showed, and no probe could:**
`hero_inspect_dante_v3.png`, card 2 reads `PROTECTIO / N` beside `· 62s CD (4s / DURATION)`, and
card 3 reads `AREA / BURST` beside `· OBJECTIVE / CHARGE`. **The kind and the timing shared one
`HorizontalLayoutGroup` with `childControlWidth`**, so they split a 266 px column between them and
both wrapped mid-word into each other.

⚠️ **STACKED RATHER THAN SHORTENED, BECAUSE SHORTENING ONLY MOVES THE THRESHOLD.**
`TSINELAS BUFF · 2 CHARGES · 10s` is 31 characters and no wording of those two facts fits one
266 px column at 18 units for every ability in the game. One fact a line always fits, and it reads
in the order the player asks: what IS this, then how often do I get it. The pair costs 44 px where
the row cost 24, and the card's body reserves 108 px for four lines that no ability uses.

⚠️ `DURATION` and `OBJECTIVE CHARGE` are gone with it. The picker already prints `62s CD · 4s` and
`ULTIMATE` for the same two facts, and two spellings of one fact on two screens is `VISION.md` § 3's
three layers drifting at the seam.

⚠️⚠️ **AND STACKING THEM EXPOSED A THIRD ONE, WHICH IS THE SAME TRAP THE 2026-08-29 PASS RECORDED
ONE LEVEL UP.** `hero_inspect_dante_v4.png`: the amber timing line drawn straight through the first
line of the body. `TopSection` was held at 52 px for a stack that now holds 28 + 22 + 22, and a
`VerticalLayoutGroup` with `childControlHeight` does not resize its own box, so the surplus paints
over whatever is under it. The old note on `card.Name` says *"a text box sized to its font size is
not a text box that fits its text"*; **this is a container sized to yesterday's contents.** 74 now.

### 132.8 ✅ The probe was reporting a blur the game does not have

🧑, of `hero_inspect_zack_v5.png`: *"the text seems very blurry"*. ⚠️⚠️ **IT WAS, AND THE GAME IS
NOT.** The live panel is on `Hud`'s canvas, which is `ScreenSpaceOverlay` with
`pixelPerfect = true` (`Hud.cs`, and `MenuKit.BuildCanvas` for every menu), so in a player every
glyph lands on a whole pixel. `HeroUiProbe` has to flip the canvas to `ScreenSpaceCamera` to get it
into a `RenderTexture`, which is the only way, **and it left `pixelPerfect` at its default of
false**: every label then sits at a fractional offset and legacy `Text` resamples its atlas across
two pixels. `tools/shoot_charselect.ps1`'s header records the same trap one screen over and is why
that tool photographs the built player instead.

⚠️⚠️ **A RENDER THAT REPORTS A FAULT THE CODE DOES NOT HAVE IS THE WORST KIND**, and it is the
mirror of `CLAUDE.md` § 6.2b: that section is about photographing the wrong screen, and this is
photographing the right one badly. Both end with somebody arguing about a picture instead of the
game.

Two fixes. `pixelPerfect` goes on, and the texture is captured at **2x** while the `CanvasScaler`
reference stays at the tray's own size, so the scale factor becomes 2 and legacy `Text` rasterises
its glyphs at twice the size rather than being scaled up afterwards. That is a retina screenshot of
the real layout rather than a magnified one, and **no rect, font size or gap moved**.

⚠️⚠️ **AND THE SHARP CAPTURE THEN SHOWED THAT HALF THE BLUR WAS REAL AND WAS SYNTHETIC BOLD.**
`hero_inspect_zack_v6.png` at 2x: the BODY is crisp and the two caption lines above it are smeared,
in the same frame, at the same scale. **DarumadropOne ships one weight.** `FontStyle.Bold` has no
bold face to reach for, so legacy `Text` fakes it by drawing each glyph again at an offset, and at
18 units that is a smear rather than a weight. The kind and the timing lines are regular now; the
name keeps its bold because it is 21 units and it is the one thing on the card. **Bold everywhere
is bold nowhere.** The two lines were never separated by weight anyway: muted cream for the job,
amber for the timing, in that order on every card.

⚠️ **FIVE ITERATIONS, FIVE RENDERS, AND ONLY THE RENDER FOUND ANY OF THEM.** v2 was the stale
frame, v3 was the wrapping, v4 was the overlap, v5 was the clipped descenders, v6 was the synthetic bold, v7 is the tray.
`CLAUDE.md` § 6.5: *"take the picture, then take it again."*

### 132.5 ⚠️ WHAT IS STILL OPEN

- ⚠️ **Nine of the twelve alternates change numbers and three change behaviour.** `dante.1.tremor`
  sweeps feet, `dante.2.plating` makes you walk, `nemu.1.fade` is a `HasVariant` branch; the rest
  scale a radius, a duration or a speed. 🧑 2026-09-02 asked for *"each loadout skill to feel
  thoroughly unique and actually add value and feel like a niche kit"*, and § 108's own note says a
  player cannot feel 25 per cent of a knockback. **This is the deepest version of the complaint and
  it is a balance change**, so it wants `BotBehaviourProbe` runs either side and § 16's arithmetic
  for how many an arm has to buy, not a Friday afternoon.
- ⚠️ **`FontStyle.Bold` IS A SMEAR EVERYWHERE IN THIS FRONT END AND ONLY THE TRAY HAS BEEN
  SWEPT.** § 132.8: DarumadropOne ships one weight, so every `fontStyle = FontStyle.Bold` in the
  UI is legacy `Text` drawing the glyph twice at an offset. `grep -rn "FontStyle.Bold"
  Assets/TumbangPreso/Runtime/UI/` is forty sites. It is defensible on a heading and it is a
  legibility cost on anything at `MenuKit.MinReadableUnits`, which is most of them. **This wants
  one pass with a render either side, not sixty edits on a hunch.**
- ⚠️ **`AbilityIcons.LabelFor` now returns a job word and `AbilityDeckHud.GetGlyphLabel` also
  calls it.** Nothing draws that today (`AbilityDeckHud` is 55 lines of helpers), so the change is
  inert there, but the next thing that shows a deck label gets the family word rather than the
  ability name and should want it.
- ⚠️ **The board has never been photographed with the ultimate row on it.** § 132.2 is asserted by
  `LoadoutSurfaceProbe` and asserted is not READ: `CLAUDE.md` § 6.2a, a green layout probe is not a
  good screen. The next render after this entry is the one that answers it.

---

## 131 · Replace Hero Strike VFX and synthesised SFX from the licensed source list ⚠️⚠️ IN PROGRESS, 2026-09-03, branch `abilities-rework`

🧑 asked for an asset hunt because the ability presentation is still the largest visual gap, then
corrected the scope when it drifted: **the existing Kenney characters stay. The eighteen Hero
Strike abilities and their sound cues are the focus.** Map and building candidates may be kept for
later, but they may not displace the ability pass.

[`Asset_Sourcing.md`](Asset_Sourcing.md) is the verified source list. It maps each ability to a
specific free source effect, lists the CC0 audio recordings and pack sources, separates public-repo
safe art from compiled-game-only libraries, carries the credit lines, and preserves later map art
without recommending character replacement.

### 131.1 The style is the game's, not the downloaded pack's

⚠️⚠️ **NO PREFAB ARRIVES WITH AUTHORITY OVER THE LOOK.** Keep `Toon.shader`,
`ToonTransparent.shader`, `WorldOutline.shader`, the warm palette and `VISION.md` § 2's readability
budget. Imported PBR, Shader Graph, VFX Graph, photographic smoke, realistic elemental simulation,
distortion, bloom-dependent effects and large white flashes do not ship. Source art is repacked
into shared atlases and materials so six heroes still look like one game.

The existing slab, wall, fissure, funnel and corridor geometry carries gameplay shape. Flipbooks,
small meshes and particles replace the primitive-looking surface and transient layers without
changing collision, range, authority or balance.

### 131.2 Done looks like

- All eighteen abilities use the mappings in `Asset_Sourcing.md` or record why a sourced piece was
  rejected after an in-engine comparison.
- Seismic Stomp, Permafrost Sheet, Flame Rush, Thunderstrike, Devouring Seance and Hex establish
  the six family looks before their siblings are converted.
- Every replaced cue is a short, dry 44.1 or 48 kHz asset, mono when positional, and no generated
  placeholder remains for a cue the pass claims complete.
- `AbilityShowcaseProbe` passes and every frame is inspected at the gameplay camera. The lata,
  chalk and players remain readable, and no effect exceeds 12 percent white.
- `AudioCueCheck` reports no fileless or unreachable cue introduced by the replacement.
- Every imported source carries its licence beside it. CC BY assets that ship are added to the
  reachable credits screen. Asset Store and Sonniss source libraries never enter public Git.
- The existing Kenney characters are untouched.
- Ilalim ng Tulay replaces the distant north boundary van at `(-2.8, RoadTop, 30.0)` with an
  optimised, warm-toon jeepney from the CC BY source in `Asset_Sourcing.md`. It replaces traffic
  rather than adding to it, remains outside the gameplay walls, passes `MapGeometryCheck`, and is
  added to the reachable credits screen.
- Other map and building work begins only after the VFX and SFX acceptance above is green.

### 131.3 ✅ THE SOURCED ART IS IN, AND FIVE OF THE SIX FAMILIES ARE WIRED, 2026-09-03, branch `abilities-rework`

**What arrived.** `tools/fetch_asset_sources.py` rebuilds the download cache from
`Asset_Sourcing.md` § 2 and § 5.1 without an account: Kenney's six CC0 packs off the asset pages,
seven OpenGameArt attachments, and PVFX Foundry through itch.io's three-request free-download flow
(the flow is not guessable and the file records it). The cache is gitignored;
`tools/build_vfx_sheets.py` writes **twelve 320 KB derivatives** into
`Assets/TumbangPreso/Resources/Vfx/` with their licence beside them in `SOURCES.txt`.

**⚠️⚠️ EVERY SHEET IS RECOLOURED AND THAT IS NOT A PREFERENCE, IT IS `UiTheme`.** The sources
arrive cobalt blue, cornflower and orange; the heroes are `HeroMagmaCore` amber, `HeroIce` teal,
`HeroFire` red, `HeroElectric` yellow and `HeroSpirit` purple. Dropping a pack in as delivered
would put two Zacks in the game, a yellow one in the HUD and a blue one on the floor. The recolour
is exact-match against a recorded palette per sheet, so a pack update stops the run instead of
silently restyling six heroes.

**What is wired, one representative per family, each judged from a render rather than from code:**

| Hero | Where | What replaced what |
|---|---|---|
| Dante | `SpawnCrackedLavaDecal` | `earth-rupture` at 1.6x the radius. The scar was always there on frame one and nothing drew the half second the road came apart |
| Cheska | `SpawnIceSheet` | `frost-nova` as the formation transient, 1.5x the radius, a sibling so the thaw cannot take it |
| Sean | `SeanHeroKit` Flame Rush | `ember-jet` at the leading edge, `Facing.Fixed` and yawed to his heading so it reads as a streak rather than a puddle |
| Zack | `CreateThunderstrike`, `SpawnLightningBolt` | **twelve `PrimitiveType.Cube` sparks WITH COLLIDERS** and **twelve `Cylinder` bolt segments**, both gone. Drawn `electric-impact` and eight hdst strokes |
| Nemu | `SpawnKuroUnbound` | `void-implosion` in the air over the maw's throat |

**⚠️ PHAISTER IS REJECTED AFTER AN IN-ENGINE COMPARISON, WHICH § 131.2 ALLOWS, AND THE REASON IS
`DrapeToGround`.** Her ward is conformed to the road per vertex because 🧑 reported the ultimate's
circle by name (*"her magic circle doesnt draw over the sidewalk and thats weird af"*), and a four
vertex quad has nothing to conform with. Her kit is also **the only one in `HeroHazards.cs` with no
`CreatePrimitive` in it** (`grep -n CreatePrimitive` returns twenty and none are hers), so § 131 has
no primitive layer here to replace. `tools/build_vfx_sheets.py` carries the argument and the route
back in, which is a subdivided UV plate rather than a decal. **That route is open work, below.**

### 131.4 Four faults the renders found and no test could

Each of these was invisible in the source and obvious in a frame, which is `CLAUDE.md` § 6.1.

1. ⚠️⚠️ **`Sprites/Default` SILENTLY IGNORES TILING AND OFFSET.** `ability_ice_sheet_eye_v51.png`
   is Cheska's nova drawn as **five novas in a row**: the whole 5 x 3 sheet on one quad. Unity's
   sprite shader passes `v.texcoord` to the sampler and never applies `_MainTex_ST`, so
   `mainTextureScale` did nothing. `Shaders/VfxFlipbook.shader` is forty lines of this
   repository's own and one `TRANSFORM_TEX`; it is in `GameBuilder.EnsureRuntimeShaders` because
   the fallback draws the bug rather than magenta.
2. ⚠️⚠️ **`AbilityShowcaseProbe.Solo` NEVER SWEPT WHAT A SPAWNER MADE BESIDE ITSELF.** Formation
   transients are siblings on purpose (`VolcanicCooling.SinkDepth` takes a zone under the road at
   the end of its life). `Object.Destroy(go, t)` never comes due in edit mode, so Cheska's nova
   survived into Nemu's frame, then the barricade's, then Dante's: **three captures in the v51 set
   are photographs of somebody else's ability.** `Clear` sweeps to a scene baseline now.
   `Transient` had solved this for itself in 2026-08-26 and `Solo` had not.
3. ⚠️⚠️ **RANK-BASED RECOLOUR LOSES VALUE, WHICH IS THE ONE THING PIXEL ART READS BY.**
   `frost-nova` is 36, 75, 124, 185, 232, 232, 253: three of seven at the bright end. Spreading
   them evenly across the ramp put the nova's main body at 86 and `ability_ice_sheet_eye_v52.png`
   is a near-black dome sitting in a hole in Cheska's own ice. The position on the ramp is the
   source colour's own luminance now.
4. ⚠️⚠️ **THE DARKEST STOP IS THE ONE THAT KEEPS BEING WRONG.** Every source sheet spends its
   largest single share of pixels on its own dark ink: 36 per cent for `frost-nova`, 33 for
   `electric-impact`, 37 for `void-implosion`. That is a shadow inside a bright drawing, and a
   near-black turns it into a hole in whatever the effect stands on. **Only `earth` and `ash` may
   go properly dark, because only those two are drawn against nothing but asphalt.**

Two more that the renders settled by measurement rather than argument: Zack's bolts were **24 m of
a 64 x 512 stroke at 0.9 m wide**, a 26 to 1 stretch against the art's own 8 to 1, and came out as
smooth ropes; the width is derived from the length at the sheet's aspect now and the reach is 12 m.
His ground contact moved twice, 3.6 m to 2.4 m to 3.4 m, because at the first it covered the shock
star and at the second it was a speck inside it.

### 131.5 ✅ The source pass changed twenty-seven cues; twenty-four remain, and the rest are a credential

`tools/build_ability_audio.py`. Every output is mono 44.1 kHz WAV, trimmed, tail-faded, capped
where the recording was longer than the cue it replaces, and **normalised to the peak of the file
it replaces** so `AudioCues.TrimDb`'s measured mix is not quietly undone one cue at a time.

⚠️ **THREE OF THESE WERE LATER RESTORED TO THEIR PREFERRED PRE-PASS FILES.** § 131.5b records the
played judgement and protects them from the generator. The processing contract here applies to the
twenty-four sourced replacements that remain.

⚠️⚠️ **THE ELEMENTAL HALF IS BLOCKED ON AN ACCOUNT AND NOT ON EFFORT.**
`Asset_Sourcing.md` § 5.2 names sixteen specific CC0 Freesound recordings for fire, ice, thunder,
rock and dark magic. **Freesound requires a login to download**: every one of those URLs answers
302 to `/home/login/`. Creating an account is not something this toolchain may do.
`Attention.md` § 11 is the ask. Until then the eighteen `sfx_cast_*` and twelve `sfx_var_*` keep
their synthesised placeholders, deliberately: replacing an ELEMENT with impact foley would make six
heroes sound like one, which is the opposite of the point.

⚠️ **The Kenney jingles are declined rather than missed.** § 5.1 offers them for round win, loss and
match win; the pack is 8-bit NES, pizzicato, sax and steel. This game is a Filipino street in carved
wood and warm cream, and a chiptune win sting is a different game's voice. Music is `Attention.md`
§ 4.

### 131.5b ✅ THREE OF THE TWENTY-SEVEN WERE REJECTED BY EAR AND THE OLD CUES ARE BACK

🧑, after playing the sourced-audio build: *"that prompt u gave replaced some good sounds we had
like can hit sound or the button hover sound or can down sound pls put those sfx back"*.

Those are `lata_impact`, `ui_hover`, and `lata_knockdown`; `AudioCues.Aliases` routes
`can_knockdown` to the last one. All three WAV files are restored byte-for-byte from the parent of
the source pass. They are also removed from `build_ability_audio.py.REPLACEMENTS` and recorded in
its `KEPT` table, so rebuilding the other sourced cues cannot overwrite them again.

⚠️ **THE OTHER TWENTY-FOUR ARE NOT ACCEPTED BY ASSOCIATION.** This report named three sounds, not
the whole batch. The exact changed list is recoverable from commit `ee8bced`, and every remaining
cue still needs the same played comparison. A pack name is not evidence that its version is better.

🧑, after the first three were restored: *"i might want to revert some so get ready for it next
time but for now thats good enough"*. `Asset_Sourcing.md` § 5.5 is the rollback ledger: the old
blobs are at `ee8bced^`, all twenty-four provisional targets are named, aliases are resolved to
their real files, and a restored cue must move from `build_ability_audio.py.REPLACEMENTS` to
`KEPT` in the same commit. Restore named files only. Do not roll the whole source pass back and
take accepted VFX or audio with it.

⚠️ **THIS LEDGER IS LINKED FROM BOTH MANDATORY ENTRY POINTS NOW.** `CLAUDE.md` § 6 sends every
session to `Asset_Sourcing.md` § 5.5 before a restore, and `Attention.md` § 13 keeps the listening
decision on the list of things only 🧑 can settle.

### 131.8 ⚠️ OPEN: `CustomGameScreenProbe` HAS NOW RUN, AND ONE CASE IS RED AT ONE RESOLUTION

**It had never been executed.** Five cases, nine resolutions, written and never run. It runs now:
**four of the five pass**, including the two that press the real door and the two that check the
ranked line and the conditional rows. The handoff that asked for it predicted *"first-run faults in
the probe's own text-matching helpers rather than in the screen"*, and the helpers were fine.

**The one red, deterministic, at exactly one shape:**

```
16:9 720p custom game: 'Label' reading "No bots · open to anybody with the code"
                       needs 306 px and was given 16.
```

⚠️⚠️ **16 px IS NOT A NARROW COLUMN, IT IS A RECT THAT HAS NOT RESOLVED.** `UiRows.Section`'s
shut-group summary is anchored from `ValueColumn` 0.56 to the right margin less `SidePadding` 28,
so `16 = 0.44 · W - 28` puts the section header's own width at about **100 px**. The list is not
100 px wide at 720p; the other eight resolutions measure it correctly and pass.

⚠️ **A LAYOUT-SETTLING FIX WAS TRIED AND DID NOT CLEAR IT.** `Resize` now calls
`Canvas.ForceUpdateCanvases`, rebuilds every `LayoutGroup` immediately and updates again, which is
strictly more correct than the three `yield return null` it replaced and is kept. The red is
unchanged, so **this is not a frame-count problem** and the next reader should not spend the
afternoon adding waits.

**What to do next, in order:** print the section header's rect width and its parent chain at each
resolution from inside `Measure`, and compare 720p against 900p. It is one of two things and the
number says which: either the scroll content is genuinely unsized at the first shape the probe
visits, or the value column is honestly too narrow for a 38-character summary and
`UiRows.Section` should stack it the way it stacks an OPEN group's subtitle.

⚠️ **THE SENTENCE ITSELF IS NOT THE FAULT AND SHORTENING IT IS NOT THE FIX.** 306 against 16 is a
factor of nineteen; no wording of *"no bots, open to anybody with the code"* closes that.

### 131.6 ⚠️ WHAT IS STILL OPEN

- **The other twelve abilities.** Five families are established. Demonic Carapace, Titan Fissure,
  Ice Barricade, Glacial Nova, Ignition Cannon, Supernova, Bolt Sprint, Magnet, Phantom Veil and
  Astral Hijack still draw their own transients. `vfx_burst_v1`, `vfx_shrapnel_v1`,
  `vfx_bolthead_v1`, `vfx_bloom_v1`, `vfx_smoke_v1` and `vfx_dust_v1` are built, committed and
  **not yet placed**: the sheets exist for exactly these.
- ⚠️ **Phaister's route back in**, § 131.3. A subdivided, UV-mapped ground plate carrying the
  sourced circle drapes exactly as `WardCircle` does and would put real authored script where the
  procedural glyphs are. Mesh work, not import work.
- ⚠️ **The `ThunderShockRing` is a flat saturated yellow star** and it is the largest single shape
  in Zack's ultimate. `ability_blast_thunder_eye_v55.png`: the drawn impact reads, and the flat
  plane under it is still `VISION.md` § 2 rule 3's puddle. It is `VfxShapes.Star`, so it is a
  silhouette rather than a primitive, which is why it survived this pass.
- ⚠️ **`SpawnSeanceVoid` is dead code.** `NemuHeroKit` builds `SpawnKuroUnbound`; only
  `AbilityShowcaseProbe` still reaches the old zone. It is deliberately NOT dressed with sourced
  art (§ 130.7's `MapPreview` argument), and it should be deleted or the probe should stop
  photographing it.
- ⚠️ **The jeepney is blocked the same way the Freesound half is.** Sketchfab requires a login to
  download the CC BY model in `Asset_Sourcing.md` § 7. `Attention.md` § 11.
- ⚠️ **`MapGeometryCheck` is red on Eskinita** with nine floating props and one prop 0.79 m from
  the can spawn. **Not this branch and not this pass**: nothing here touches a map. It is recorded
  because `Checks.RunAll` now reports it on every run and the next reader will wonder.

---

## 130 · Crossplay, the boot ANR, and the lobby that was drawn in a different language ⚠️⚠️ OPEN, 2026-09-03, branch `ui-redesign`

🧑 asked three things in one sitting, and two of them turned out to be the same kind of
fault as each other: **"just wnated to ask as well if crossplay can work? make sure it does"**,
*"i want a mobile and a pc to be able to play tgthr"*, then **"i noticed the shader doesnt show up
in map select so the map select or LOBBY look IS COMPLETElY DIFF to the actual game"** and *"can u
make sure game and lobby preview looks exactly the same"*.

### 130.1 ✅ The crossplay ARCHITECTURE was already right, and nothing on the wire needed to move

Checked rather than assumed, which is § 0.6's rule and § 128's lesson about asking what CALLS a
thing. **`NetSession.ApproveConnection` reads three facts and none of them is a device**: the
protocol version, capacity, and the host's block list. A pad, a thumb and a keyboard all arrive at
`InputIntent` and nothing about which device was used goes on the wire, exactly as `CLAUDE.md`
§ 4a says. `ProtocolVersion` is untouched at **21**.

⚠️ **So "can crossplay work" was never the question. "Why does it not work on the phone" was**,
and the answer was two defects with nothing to do with the network layer.

### 130.2 ⚠️⚠️ A FAILED SIGN-IN WAS CACHED FOR THE LIFE OF THE PROCESS, AND ON A PHONE THAT IS FATAL

`NetIdentity.EnsureSignedInAsync` was one line:

```csharp
return _attempt ??= AttemptSignInAsync();
```

**The `??=` caches the Task whether it succeeded or failed.** The boot attempt fires from
`[RuntimeInitializeOnLoadMethod(AfterSceneLoad)]`, which on Android runs **while the handset is
still associating with wifi**. One bad moment at boot settled `OnlineState.Unreachable`, that
failed Task stayed in `_attempt`, and **every JOIN BY CODE, every relay host and every lobby query
for the rest of the app's life awaited that same dead answer.**

⚠️⚠️ **AND NOTHING ON SCREEN SAID SO.** `StartRelayClient` aborts on false and reports
*"could not reach the game"*, which reads as the HOST's fault. **The only cure available to the
player was force-closing the game.** § 126.11's first bullet is this fault seen from the other end:
*"the .apk has no UGS session on the device"*.

⚠️ **THE FIX IS A SPLIT BETWEEN REASONS THAT CAN CHANGE AND REASONS THAT CANNOT**, because the
cache was not wrong to exist. It was added when a session that could not reach UGS paid for
`UnityServices.InitializeAsync` and logged the same warning **21 times**.

| Settled state | Cached? | Why |
|---|---|---|
| `SignedIn` | **for ever** | Asking again cannot improve it, and a second `SignInAnonymouslyAsync` beside a live one is what UGS answers with *"The player is already signing in"* |
| `NotLinked` | **for ever** | A property of the BUILD. Retrying prints the same sentence per call, which is the 21-warnings fault |
| batch mode | **for ever** | Same: a property of the run |
| `Unreachable` | **no**, retried after `RetryCooldownSeconds` = 5 s | A property of the MOMENT, and the next moment is a player pressing JOIN with the wifi up |

- **`NetIdentity.CanRetrySignIn`** is the predicate, so a screen can decide whether to offer a TRY
  AGAIN at all. `CLAUDE.md` § 6.3: a control that does nothing when pressed must not look
  pressable, and a TRY AGAIN on an unlinked build is a dead end.
- **`RetrySignInNowAsync`** ignores the cooldown, for a person who actually pressed something.
- ⚠️ **THE RATE LIMIT IS THE HALF THAT STOPS THIS BECOMING THE FAULT IT REPLACED.** A screen
  polling `EnsureSignedInAsync` every frame would otherwise turn one dropped connection into a
  request flood against the one service this game cannot do without (`FUTURE.md` § 19.7).
- Four tests in `NetworkMultiProcessProbes`, one per row of that table plus the rate limit.

### 130.3 ✅ The second UGS sign-in in the test suite, which is § 126.11's other bullet AND part of § 126.8

`UgsServicesProbe` went red **six times in one full PlayMode run** with *"You are not signed in to
the Authentication Service"* and *"The player is already signing in"*, having passed an hour
earlier with nothing changed.

⚠️⚠️ **BOTH MESSAGES ARE ONE FAULT AND IT WAS THE PROBE'S.** `NetIdentity.SignInAtBoot` fires
one anonymous sign-in the moment PlayMode starts. The probe then started a **second one beside
it**; UGS refuses a concurrent sign-in with *"already signing in"*, after which **neither has
completed**, so the next assertion reads *"not signed in"*. Whether the two raced depended on how
long the suites ahead of it took, **which is exactly why the suite moved between two runs of the
same code**. That is § 126.8's cross-test leakage class, in the one place tearing a scene down
cannot reach: a shared cloud session.

- `UgsServicesProbe` and `MatchRecordIdentityProbe` now go through `NetIdentity.EnsureSignedInAsync`,
  whose whole design is that a caller arriving mid-attempt awaits THAT attempt.
- ⚠️ **`CloudEndpointActionProbe` DELIBERATELY STAYS A DIRECT CALL** and now says why: it has
  just switched to `ProbeProfile` precisely so its writes cannot land on a real player, and the
  shared path would sign in on the game's profile and undo that.

### 130.4 ⚠️⚠️ CASUAL QUICK MATCH BANDED BY PLATFORM, SO A PHONE AND A PC COULD NEVER MEET THROUGH THE FRONT DOOR

`MatchmakingRules.PoolKey` was `v{protocol}.{mode}.{stake}.{device}.{platform}` for **both**
stakes. `CLAUDE.md` § 4a has said, in these words, since the crossplay paragraph was written:
*"Crossplay is for lobbies, join codes and LAN; the **ranked queue** still bands by device."*

⚠️ **THIS IS THE CODE CATCHING UP WITH THE RULE, NOT A NEW DECISION**, and it is the shape
`CLAUDE.md` § 5 describes: prose and code disagreed and the disagreement is being named rather
than silently resolved. **The argument the banding protects is a RANKED argument.** `FUTURE.md`
§ 14: *"No aim assist. Separate the pools instead, which is free, exact, and removes the
argument."* Nobody disputes a casual match, and a casual queue that refuses to seat the two people
who own the two devices in the room is protecting nothing and costing the game its stated feature.

- **Casual is `v21.Classic.Casual`, three parts.** Ranked keeps all five.
- ⚠️ **THE FIELDS ARE DROPPED, NOT SET TO A PLACEHOLDER.** A key reading `.Any.Any` is still a
  key with two fields in it, and the next person to add one would have to work out which of three
  spellings of "not banded" this was. Three parts and five parts cannot be confused.
- `ACasualQueueSeatsAPhoneBesideAPc` and `ARankedQueueStillBandsByDeviceAndPlatform`, both sides.

### 130.5 ✅ `Shader.WarmupAllShaders()` is gone, which is § 126.10's ANR

**It is why the phone never reached the menu, so it was on the crossplay path rather than beside
it.** One blocking call compiling every variant in the build, in the ONE stage of
`SplashScreen.PreloadGameAssets` that could not yield, in a routine whose own header says *"IT
YIELDS BETWEEN EVERY STAGE, DELIBERATELY"*.

- **`ShaderVariantCollection.WarmUpProgressively` is the only incremental warm-up Unity exposes.**
  `Shader.WarmupAllShaders` and `ShaderVariantCollection.WarmUp` are both all-or-nothing. It needs
  a collection asset, so the asset is the mechanism rather than a convenience.
- **`ShaderWarmupCollection` (editor) GENERATES it from every material in the project**, on every
  build and in `Checks.RunAll`. ⚠️⚠️ **NOT `ShaderUtil.SaveCurrentShaderVariantCollection`**,
  which saves the variants seen since tracking started: the ones somebody happened to walk past in
  one play session. **A screen nobody opened contributes nothing**, which is `CLAUDE.md` § 4a's
  § 96 and § 124.11 fault arriving in the renderer.
- **10 shaders per frame**, so Android's five-second ANR watchdog never sees a blocked main thread,
  and the bar moves inside the stage rather than only at its boundaries.
- ⚠️ **A MISSING COLLECTION WARNS AND DOES NOT FALL BACK TO `WarmupAllShaders`.** Falling back
  would reinstate the ANR on the one platform that cannot survive it, invisibly, on exactly the
  build where generation failed. It is a build gate for that reason.
- ⚠️ **The asset is gitignored**, on `BuildBranch.txt`'s precedent and for its reason: a file whose
  whole job is to be rewritten by a tool is a merge conflict waiting for whoever builds next.
  Absent is a safe state and a loud one, and it is never absent in a shipped player.

⚠️⚠️ **AND THE FIRST VERSION OF THE GENERATOR WAS A REGRESSION WEARING THE FIX'S CLOTHES, WHICH IS
THE PART OF THIS WORTH READING.** It walked MATERIALS, which is the obvious way to enumerate
shaders, and the measurement was:

| | Shaders | Variants |
|---|---|---|
| Material walk only | **5** | 20 |
| Plus `Shader.Find` literals, plus authored `.shader` assets | **23** | 53 |

**This project has EIGHT `.mat` assets in `Assets/` and builds essentially every material it draws
with in code** (`GodotTheme.Box`, `PaperCraft`, `WoodCraft`, every VFX builder), while the runtime
names **nineteen** shaders by string. So the material walk measured the handful of authored
materials and missed the whole game.

⚠️⚠️ **AND IT WOULD HAVE BEEN INVISIBLE.** The loading bar still moves, the ANR is still gone, and
the hitch simply comes back later, during a round, which is exactly the fault
`SplashScreen.PreloadGameAssets`'s own header was written about: *"the work did not disappear, it
just happened at the worst possible moment."* **The shader count is printed in the log line for
this reason** — 5 and 23 are very different answers and only one of them was ever on screen.

⚠️ **THE SOURCE OF TRUTH IS THE `Shader.Find` LITERAL, READ AS TEXT**, which is `SceneScriptCheck`
and `InputSurfaceCheck`'s method for their reason: a shader nothing instantiated during a scan is
still a shader the player meets. ⚠️ **It also reports a name that does not resolve**, which is a
pink-material bug in the shipped player rather than a warm-up problem, and this is the only place
in the project that looks. It is **0** today.

### 130.6 ⚠️⚠️ THE LOBBY AND MAP SELECT WERE DRAWN WITHOUT THE INK OUTLINE, AND THIS IS THE THIRD CAMERA IT HAS HAPPENED TO

🧑's report is exact. `CameraRig.Awake` installs **three** passes: `ColourGrade`,
`PostAntiAlias` and `WorldOutline`.

| Camera | Got | Missing |
|---|---|---|
| `CameraRig` (the match) | all three | — |
| `SpectatorCamera` | the first two | `WorldOutline`. **Already found and fixed once**, and its header spends twenty lines on why nobody noticed |
| `MapPreviewSurface` (**the lobby and the map select**) | the FIRST only | `WorldOutline` |
| `MapPreview` (unreferenced, see § 130.7) | none | all three |

⚠️⚠️ **THE GRADE IS WHY IT LOOKED WRONG RATHER THAN MERELY DIFFERENT.**
`MapPreviewSurface.ApplyEnvironment` already copies the arena's ambient, fog, skybox and
`MapGrade` onto the preview camera, **so the preview had the right COLOUR of the game and none of
its LINE.** `docs/VISION.md` § 6: *"anything drawn in a different visual language is the thing
that looks broken"*, and an inked world beside an un-inked one is two visual languages on the two
screens a player crosses between most often.

- ⚠️ **`PostAntiAlias` IS STILL DELIBERATELY ABSENT AND THAT IS NOW ASSERTED.** `CameraRig`
  names this camera when it explains why: the preview renders into a `targetTexture` already built
  with 4 samples, so filtering it would soften a picture that is not aliased. **Two of three is the
  right answer here and the reason is written down; one of three was not.**
  `ThePreviewDeliberatelySkipsAntiAliasingBecauseItsTargetIsAlreadyMultisampled` encodes the
  exemption, which is § 126.13's lesson applied before it becomes § 126.13.
- **`WorldCameraPassParityTests` reads the three sources as TEXT**, for `SceneScriptCheck`'s reason:
  every runtime probe can only measure a camera that was BUILT during a run, and each of these is
  built by a screen somebody has to open.

### 130.7 ✅ `MapPreview.cs` was dead code that looked exactly like the live class, and it is gone

`TscnUiImporter` line 676 attaches **`MapPreviewSurface`** to the node named `MapPreview`, and
`ConvertedMatchSetup` reads `MapPreviewSurface` off that node. **Nothing constructs
`MapPreview`.** The two share a name and nothing else, and the difference is invisible from the
file name: one copies the arena's environment and installs two passes, the other renders the arena
flat.

**A session that "fixed the map preview" in that file would change nothing a player can see**,
which is the most expensive kind of fix. Its header now says so in full. **Done looks like** a
decision to delete it, with its camera notes (the sway-not-orbit argument is the original
conversion of `map_preview.gd` and is worth keeping somewhere) moved onto `MapPreviewSurface`
first. ⚠️ **Do not delete it without moving those notes**; `CLAUDE.md` § 3 asks for the reasoning
to survive the deletion.

**Deleted 2026-09-03, notes first.** Three things moved onto `MapPreviewSurface` before the file
went, and the first of them was the only copy in the repository:

- ⚠️⚠️ **THE SWAY IS THE OPPOSITE OF THE CHARACTER PREVIEW AND THE TWO MUST NOT BE UNIFIED.**
  `MapPreviewSurface` already carried the sway-not-orbit argument; what it did not carry is the
  comparison. A character is one object that reads from every side, so turning the MODEL shows the
  whole thing; an arena is a room with a front, so turning the CAMERA through it shows the player
  the back of a set. They look like one feature and a shared "preview spinner" would have to pick
  one, breaking the other screen. Nothing else in the tree said this.
- The blueprint-grid placeholder the preview replaced, and why it had outlived its job: by the
  time two dressed maps existed, the screen was asking the player to choose between two names on a
  backdrop that showed neither.
- The ambience note onto `MapPreviewSurface.Silence`, which had the code and not the reason. Every
  map carries a player set to play on awake, so loading one starts the street bed over the menu
  and cycling the picker restarts it on every press.

⚠️ **`WorldCameraPassParityTests`' third bullet KEEPS naming it**, deliberately: two classes one
letter apart in the file browser, one live and one dead, is how a session spends an afternoon
fixing a preview that nothing constructs. The bullet now says it was deleted and says not to
reintroduce a second preview class. `WorldOutline`'s far-plane note cited its 400 and no longer
names it.

### 130.8 ⚠️ NOT DONE: nobody has still watched a phone and a PC join each other

§ 126.11 stands, and this batch removed the two things sitting in front of it rather than doing
it. ⚠️⚠️ **THE EMULATOR CANNOT ANSWER THE LAN HALF AT ALL** and that is worth writing down
before somebody spends a session on it: the AVD sits behind a NAT on `10.0.2.x`, so it cannot
receive the host's LAN broadcast and `LanBeacon` can never see it. **The relay join code is the
path that can be demonstrated on this machine.** ⚠️ **And a real handset may need a
`WifiManager.MulticastLock` for the LAN browser to receive broadcasts at all**, which is untested,
unbuilt, and needs a device rather than a guess.

---

### 130.9 ✅ § 129.3: the YOU card row is measured now, on its fourth report

The mechanism, not another shorter string. `resizeTextForBestFit` **looks** like a measurement and
is not one a caller can read: when the text still does not fit at `resizeTextMinSize` it does not
report that, it clamps at the floor and `horizontalOverflow = Overflow` draws the surplus into the
neighbour. `MenuKit.Fit` measures the same thing and **answers**, which is what lets
`FitIdentityRow` act on a failure instead of drawing one.

- **The 170-unit role column was the trusted number and is a measured one now.** `170 + 10 + 140 =
  320 against 336` was correct arithmetic at ONE canvas width, and `AspectSafeCanvas` scales on the
  short axis, so 336 was never the number on his window. `CLAUDE.md` § 6.2c row 1.
- ⚠️ **`detailLayout.minWidth` GOES TO ZERO AND THAT IS A REVERSAL WORTH READING.** A
  `HorizontalLayoutGroup` will not shrink a child below its `minWidth` and **overflows the
  container instead**, so the 140-unit floor protecting the name was also one of the two things
  making the row overrun. The name is the child that gives; the role is capped at half the row.
- ⚠️ **AND THE LAST-RESORT CASE CLIPS RATHER THAN OVERPRINTS.** When `Fit` returns false the name
  goes to `Wrap` + `Truncate`, which on a one-line rect is a clip. **Half a string is a bad
  readout; two strings on top of each other is neither.**

### 130.10 ✅ § 126.8: the five named fixtures tear their world down, and the cause was one line

⚠️⚠️ **THE MECHANISM IS `SceneManager.LoadSceneAsync(name, LoadSceneMode.Single)` IN FOUR FILES,
AND NOT ONE OF THE FIVE SUITES § 126.8 NAMES BY STACK TRACE HAD A TEARDOWN OF ANY KIND.** A
single-mode load destroys every object in the previous scene, so the scene a test loaded was still
active when the next test started, and a load still settling was settling into the next test's
objects.

**`SteeringTests` is the clearest reading of it.** The failing test builds a bare `Cube` floor and a
`CharacterMotor` in the ACTIVE scene, waits twenty fixed updates, then reads `go.transform` at line
177. A single-mode load left in flight by the test before it lands during those twenty frames and
takes both objects with it. That is the `MissingReferenceException` **inside the test**, and it is
the same shape at `SettingsWheelProbe.cs:117`, `UiClickProbe.cs:140` and `VolcanicZoneTests.cs:60`.
*"the arena built no SliceRunner"*, *"No main camera in the arena"* and the rest are the same cause
seen from the other side: a scene asked for while another load is pending.

- **`PlayModeWorld.Reset`** creates an empty scene, unloads every other loaded scene **including
  additive ones**, and waits for each `AsyncOperation` to report `isDone`, so nothing is ever in
  flight when it returns.
- ⚠️⚠️ **IT ENDS THE LIVE MATCH FIRST, AND THAT IS `SoloPracticeTests`'S OWN FINDING
  GENERALISED.** Its teardown already recorded the half a scene unload cannot reach: **the
  directors are `DontDestroyOnLoad`, so a live round outlives the scene**, and the next suite's
  arena loads underneath a match that is still ticking. *"`LandedHighlightTests` failed exactly
  that way, twice, and passes alone."* One suite had noticed the class, fixed its own instance, and
  there was nowhere to put the general version. Now there is.
- **Both hooks on all five**, not just teardown: until every fixture in the folder has the pair,
  there is always something ahead of a suite that does not.
- ⚠️ **This is option 1 of the two § 126.8 offers and deliberately not option 2.** No third
  category exclusion, no widened bound.
- ⚠️ **It does not reach `UgsServicesProbe` and the entry says so.** That is § 130.3, and it is a
  different fix: the probe was starting a second UGS sign-in beside the one `NetIdentity` fires at
  boot.

⚠️⚠️ **THE HEADING STAYS OPEN UNTIL A FULL RUN SAYS OTHERWISE, WHICH IS THE ENTRY'S OWN
STANDARD.** § 126.8 exists because a number from one run was quoted as a gate. **The cause is
identified and the five named suites are fixed; the claim "the suite is a gate again" needs a full
run to compare against 42 and 41, and that run has not been done on this commit.** Do not close
§ 126.8 on this entry.

### 130.11 ✅ § 126.13: two of the three `Fit(..., 14)` floors did not need an exemption at all

The entry asked for *"a decision, taken with a render in hand: either the pills get wider and the
floor goes to `MenuKit.MinReadableUnits`, or the exemption is written into the probe by name"*, and
closes **"Do not lower the probe's floor to make it green."** All three answers are here and the
probe's floor is untouched.

| Site | What it turned out to be |
|---|---|
| **line 1205, the variant tile** | ⚠️⚠️ **A BUG.** `band - 86` reserved 86 units for the `EQUIPPED` mark, and that mark is built inside `if (equipped)`. **Every unequipped tile was squeezing its name to make room for nothing**, and most tiles on the screen are unequipped. Now `band` when not equipped |
| **line 1094, the skill board** | ⚠️ **A COLUMN WIDTH NOTHING WAS COMPETING FOR.** The row is a glyph and a text column and nothing else, and `inner` is 964 units, so 180 left about seven hundred units of board empty while shrinking a 17-character ability name to 14. It is `inner - 60 - 30 = 874` now and **the floor is `MenuKit.MinReadableUnits`** |
| **line 592, the tab rail** | ⚠️ **THE GENUINE EXEMPTION.** `MAKE YOUR OWN` beside `LATA` in cells a `HorizontalLayoutGroup` decides. It keeps its 14 |

⚠️⚠️ **AND THE EXEMPTION IS REGISTERED BY `MenuKit.Fit` ITSELF RATHER THAN BY THE CALLER, WHICH IS
THE ONLY VERSION THAT CANNOT ROT.** `TightLabel` is attached whenever a fit settles below the
readable floor, and `AspectRatioProbes` **skips it and prints it** with its floor, its settled size
and the room it was fighting for. § 126.13's cause was *"a local exemption that was copied twice and
never encoded anywhere a test could see"*; a marker the caller had to remember is a second place to
forget, and forgetting it compiles (`CLAUDE.md` § 4a). ⚠️ **It registers the RESULT, not the
request**: a caller may pass 14 and the string may still fit at 20, and recording that would pad the
list until it stopped being read.

### 130.12 ✅ Phase 12: map rotation and the map vote, which § 128.2 called the cheapest unbuilt thing

`FUTURE.md` § 12 and § 19.12 both say to build this **before a fourth map**: *"A map is the most
expensive content in the game. Map rotation and a map vote are nearly free and buy most of the same
freshness."* § 128.2 recorded that **nothing in the repository grepped for either**.

**`MapRotationRules`, engine-free**, which is § 19.12's stated constraint. Sixteen tests.

- ⚠️⚠️ **THE ROTATION AND THE VOTE ARE ONE FEATURE.** A vote answers "what do these four people
  want"; a rotation answers "what happens when nobody says". **The vote alone leaves a lobby where
  four abstentions replay the same map for ever**, which is the exact staleness the feature exists
  to remove, and a lobby where nobody presses anything is the COMMON case: four people who have
  just finished a match are looking at a scoreboard, not a ballot. `Decide` is the whole thing.
- ⚠️⚠️ **THE TIE-BREAK GOES TO THE MAP YOU ARE NOT ON, AND THE OBVIOUS RULE IS WRONG HERE.**
  "Lowest index wins" hands a 2-2 split to the CURRENT map half the time, which buys none of the
  freshness this was bought for. A majority can still keep the map it is on; it just cannot keep it
  by accident.
- ⚠️ **`NoVote` IS -1 AND NOT 0**, because 0 is a real map index and a tally that conflates "no
  answer" with "the first option" gives every silent lobby to Eskinita **and looks exactly like a
  working vote**.
- ⚠️ **A vote for a map this build does not have is DISCARDED, not clamped.** A clamp turns a peer
  on a four-map build into a vote for map 2 on a three-map build, which is a silently wrong answer
  rather than an absent one.
- ⚠️ **`OpeningMap` is derived from the week**, `MirrorIndex`'s argument exactly, with the same
  pre-epoch guard: a venue machine with a flat CMOS battery boots in 2000 and C# `%` keeps the sign
  of the left operand.

**The runtime half: a REMATCH now moves to the next map.** That is the single moment worth the most
(four people who have just agreed to keep playing, handed the identical street every time), and it
is host-only over the `SelectMap` broadcast that already exists.

⚠️⚠️ **NO NEW WIRE MESSAGE AND THEREFORE NO `ProtocolVersion` MOVE, AND THAT WAS A DELIBERATE
CONSTRAINT RATHER THAN LUCK.** Moving the protocol means the Windows player and the .apk must be
rebuilt and shipped together (`CLAUDE.md` § 4a), and this session's whole other half is 🧑 asking
for a phone and a PC that can play together. **A feature that did not need the bump did not get
one.**

⚠️⚠️ **AND THE BALLOT IS BUILT NOW, ON A BUMP SOMEBODY ELSE PAID FOR: § 130.18.** The sentence
above is still the right rule and it is worth reading beside what happened next. § 130.13 moved
`ProtocolVersion` to 22 for LAST TSINELAS's match half **in the very next commit**, so the ballot's
two messages cost nothing extra: the dual rebuild was already being done. **The rule is not "never
add a message", it is "never add one that has to buy its own rebuild"**, and the corollary is that
a bump is the cheap moment to spend everything that has been waiting for one. This had been waiting
about two hours.

### 130.13 ✅ LAST TSINELAS STANDING HAS A MATCH HALF, AND IT COST THE PROTOCOL BUMP THIS ENTRY PREDICTED

§ 128.2 says the format *"has rules, tests, a document and no match half"* and lists what the core
already gives. **What it does not say is what building the match half actually costs**, and that is
worth writing down before somebody starts it thinking it is an afternoon.

**A Last Tsinelas round ends EARLY and awards points nobody else can compute.** The last attacker
standing takes `LastStandingPoints`, so:

- the round can end on a condition other than the clock, which every peer's HUD reads;
- a new `ScoreEvent` is needed for the award, or the toast says the wrong thing;
- a peer that has never heard of either **is two different games sharing one scoreboard**, which is
  the exact sentence `NetSession.ProtocolVersion`'s own note uses to justify the move to 21.

⚠️ **21 COVERS "THE PEER KNOWS ABOUT `MatchFormat`", NOT "THE PEER KNOWS HOW TO RUN THIS ONE."**
Those are different claims and only the first one shipped. So the match half moves the protocol to
22, and `CLAUDE.md` § 4a's consequence follows: **the Windows player and the .apk are rebuilt from
the same commit and shipped together, or they refuse each other correctly and it reads as a bug.**

**Done looks like**, in this order: the round logic host-side, the `ScoreEvent`, the bump, both
players rebuilt together, and only then the format added to `ConvertedMatchSetup.FormatAt`. ⚠️ **It
stays out of the lobby's format list until the match can run it**, which § 128.2 already says is
correct: *"a format a player can pick and the match cannot run is worse than one that is not
offered."*

**Built 2026-09-03, in that order.** `Runtime/LastTsinelasDirector.cs`, and it is short because
the rules were already engine-free: `TsinelasLeft`, `IsOut`, `LastAttackerStanding` and the two
functions added beside them are asserted by `Core.Tests` in about a second, and what is left in
the runtime is the three things that genuinely need the engine (when a tag happened, which body to
switch off, and how to tell the other three machines).

- ⚠️⚠️ **EVERY TAG IN THIS GAME IS ALREADY A TAG ON A CARRIER, SO THE LOSS CONDITION NEEDED NO
  NEW CHECK.** `CustomGameRules.TsinelasLeft`'s note reads *"when the taya tags you while you are
  carrying it back"*, which looks like a second condition to test on top of the tag.
  **`CharacterMotor.IsTaggable` returns false unless `HoldingSlipper`**, and `ResolveTag` asks that
  same function before awarding anything, so a tag and a spent tsinelas are the same event. Testing
  `HoldingSlipper` again in the director would read the flag one frame after `ApplyTagPenalty` sent
  the slipper home, and **would silently never fire**.
- ⚠️⚠️ **AND THE OTHER HALF OF THAT NOTE CANNOT ELIMINATE ANYBODY.** *"A tsinelas is lost when the
  round ends with it still on the floor"* is true of the format and is not a code path: stock is
  PER ROUND (`IsOut` says "out for the rest of the round"), so a loss charged at the final whistle
  is charged to a round that is already over and would fire after the winner had been paid.
- ⚠️⚠️ **`CustomGameRules.AliveAttackers` AND `RoundIsDecided` ARE NEW, AND THEY EXIST BECAUSE
  `LastAttackerStanding` ANSWERS -1 FOR TWO OPPOSITE SITUATIONS.** Its own header says so: -1 is
  "more than one alive" and also "nobody alive". A caller holding only the slot cannot tell "carry
  on" from "end the round and pay nobody", and **`RoundIsDecided` is `alive <= 1` and not
  `alive == 1`**: writing the obvious one leaves a round with nobody left in it running the full
  90 seconds with four bodies that cannot act and no reason on screen. That is the format's worst
  possible failure and it is one character.
- ⚠️⚠️ **THE AWARD IS PAID BEFORE `BeginIntermission` AND THE ORDER IS LOAD-BEARING.**
  `BeginIntermission` sets `IsWarmupBuffer` and `AddScore` returns early on exactly that flag, so
  the two lines the other way round pay nobody, silently, on the one award the whole format exists
  to make. `LastTsinelasMatchHalfTests` asserts the ORDER of the two strings in the file, because
  a test that only asserted both were present would pass on the broken version.
- ⚠️ **THE TAYA IS PAID NOTHING EXTRA FOR CLEARING THE COURT.** `LastAttackerStanding` says a
  round with no survivors belongs to the taya; it does not say it is worth 100. They have already
  banked a `Tag` for each attacker they put out, which is 300, and a fourth award on top would make
  clearing the court worth more than the format's headline prize.
- ⚠️ **AN ELIMINATED PLAYER CAN STILL WALK.** `RoundActive` is the switch, not `FreezeForMatchEnd`:
  `CanAct` reads it (so the throw, the grab and the reset channel stop, and `IsTaggable` goes false
  so they cannot be charged a tsinelas they do not have) and `CanMove` does not. A player frozen in
  place for up to a minute with no explanation is `CLAUDE.md` § 6.3's dead end.

⚠️⚠️ **TWO DEFECTS IN THIS FEATURE WERE FOUND AFTER THE FIRST BUILD AND BOTH WERE CLIENT-ONLY, SO
NEITHER WOULD HAVE SHOWN UP ON THIS MACHINE.** They are worth reading before adding anything else
that switches a body off, because the shape is the same both times: **two files that are each
correct on their own, and only a client in a live round puts them together.**

1. ⚠️⚠️ **`RoundDirector.ApplySnapshot` UNDID THE WHOLE FORMAT AT 5 Hz.** "Out" is
   `RoundActive = false` on the body, and that method stamps `RoundActive` onto **all four**
   bodies on every replicated packet while a match is in progress. So an eliminated attacker on a
   client got the flag back within 200 ms and carried on throwing, grabbing and charging resets
   while the host ignored every request. **The host is immune by construction** (`HostSyncPeer`
   hands it its own snapshot), which is exactly why single-machine testing could never see it.
   `ApplySnapshot` asks `LastTsinelasDirector.IsOut` now, and ⚠️ **only ever to hold a body DOWN,
   never to raise one**: the guard is inside the `roundActive` branch so that when the round ends
   everybody stops together, out or not.
2. ⚠️⚠️ **THE TAYA'S SLOT WAS DERIVED ON THE PEER AND LOST A RACE ON EVERY WHISTLE.**
   `ApplyNetworkStocks` read `MatchDirector.DefenderSlot`, which is derived from the round NUMBER,
   which arrives in `SyncWorld` at 5 Hz. So there is a window where the stock packet carries the
   new round's table and the peer still holds the old round's number. **The taya's stock is 0 by
   definition**, so a peer that guessed the wrong slot reads the real taya as an eliminated
   attacker and switches their body off for the round. The slot travels in the message now, which
   is four bytes and removes the race outright rather than narrowing it.

⚠️ **The first build was thrown away for these.** The Windows player had already been built and the
`.apk` was about twenty minutes into IL2CPP; both were rebuilt from the corrected commit, because
`CLAUDE.md` § 2.2's *"a build is a claim that there is something worth looking at"* is not true of
a build whose headline feature does not work for three of the four players.

**The wire, which is the half that cost the bump.** `MatchRpc.BroadcastTsinelas` sends the WHOLE
stock table and the taya's slot rather than the decrement, on every tag and again on every whistle. ⚠️ A delta that is
dropped or reordered leaves a peer permanently one tsinelas out with no way to notice; four
integers on the handful of frames a tag happens costs less than the code to detect that drift.
`BroadcastScore` next door sends the KIND rather than the delta for the opposite reason, and both
are the same rule: **send the thing the receiver cannot reconstruct.**

⚠️⚠️ **AND A HUD-ONLY REPLICATION WOULD HAVE LOOKED FINE AND BEEN WORSE THAN NOTHING.** `RoundActive`
is a local flag. A client that only drew the counter would show an eliminated player a correct
`OUT · NO TSINELAS LEFT` while their own body still threw, grabbed and charged a reset, every one
of which the host then ignored. **That is not a wrong number, it is a player being told the game is
broken.**

**`NetSession.ProtocolVersion` is 22**, and the constant's own note carries the three reasons.
`ChatAndLobbyChromeTests` and `InputContractTests` both assert the number and both were updated in
this commit. **Both players were rebuilt from this commit and shipped together**, per `CLAUDE.md`
§ 4a.

**Only then the lobby row.** `ConvertedMatchSetup.FormatOptionCount` is 3 and `FormatAt` was
rewritten: ⚠️ it read `index <= 0 ? Standard : Mirror`, which is correct for exactly two options and
**silently maps a new middle row to `Mirror`**, offering the player a format the picker names
something else. The row index is the enum value now. ⚠️ **It costs one migration**: `settings.json`
stores the ROW INDEX, so a player who had MIRROR selected opens the lobby on LAST TSINELAS once.

⚠️ **STILL OPEN AND DELIBERATELY NOT DONE HERE: the custom lobby has no tsinelas stepper.**
`SceneFlow.SelectedTsinelas` exists, is clamped on the host, and is written by nothing, so every
match plays the shipped three. `CustomGameRules.MinTsinelas`/`MaxTsinelas` are the bounds a row
would move it between. That is a custom-games UI row and belongs with § 115's remaining work, not
with the match half.

⚠️ **AND NOBODY HAS PLAYED A ROUND OF IT.** The rules, the wiring, the wire and the picker are
asserted; what is not asserted is whether three tsinelas makes a round that ends at a good moment,
because that is a `BotBehaviourProbe` measurement over several seeded runs (§ 16's noise floor:
three runs an arm for anything worth 20 per cent). **Do that before tuning any number.**

### 130.14 ✅ CLOSED 2026-09-03: `SteeringTests.MouseAimedMovementIsRelativeToTheBody` WAS DETERMINISTICALLY RED, AND § 130.14b IS WHY

**Measured both ways rather than assumed**, because this batch put a teardown into that fixture and
a red that arrived with a change is the change's until proven otherwise:

| | Result |
|---|---|
| Current code, suite run in a group of seven | `moved.x` **1.97339928** against a bound of 2.0246408 |
| **`SteeringTests.cs` reverted to HEAD, no teardown, run ALONE** | `moved.x` **1.97339928** against 2.0246408 |

⚠️⚠️ **THE SAME NUMBER TO EIGHT SIGNIFICANT FIGURES, SO IT IS PRE-EXISTING, DETERMINISTIC AND NOT A
FLAKE.** That matters twice over: it is not caused by § 130.10's fixture work, **and it is not the
§ 126.8 class at all** — a cross-test leak produces a number that moves between runs, and this one
does not move at all.

**What it says.** A seat facing due east, pressing W for 40 fixed updates, moves `(1.97, 0.00,
1.08)`. That is **28.7 degrees north of its own forward**, not a rounding error, and the assertion
is `moved.x > magnitude * 0.9`. The seat is being steered partly in world space, which is exactly
what the test's own message says and what `CLAUDE.md` § 7.1 records for seat 0 in § 34: *"`Steer` on
the branch that reads a heading as body-relative and never rotates the body"*.

⚠️ **`AimSource.Mouse` IS THE ARM UNDER TEST** and § 34's fault was the same source on a different
seat, so read § 34 before starting: that entry cost 224 m against 522 to 556 for the sibling seats
and was found by a probe rather than by playing. **Done looks like** the cause named in
`CharacterMotor.Steer` or `CameraRig`, not a widened bound: the sibling test
`TagSelectionServesEveryEligibleSeatBeforeRepeating` and the movement-aimed case both pass, so the
two arms disagree and one of them is wrong.

### 130.14b ✅ FIXED 2026-09-03, AND THE STEERING WAS NEVER WRONG. THE TEST BURIED ITS OWN SEAT IN THE FLOOR

⚠️⚠️ **THE ENTRY ABOVE IS A CORRECT MEASUREMENT AND A WRONG CONCLUSION, AND BOTH ARE WORTH
KEEPING.** Every number in it is right: 1.97339928 against 2.0246408, identical to eight
significant figures whether the fixture is run alone or in a group, therefore pre-existing and
deterministic and not § 126.8's class. What it then infers, that *"the seat is being steered partly
in world space"*, is what the failure MESSAGE says rather than what the code does, and two sessions
took the message at its word.

**The answer needed the per-frame picture and nothing else.** `SteeringTests.
TheSteeringFrameByFrameIsWrittenOut` is a test that PRINTS instead of asserting; it writes
`Logs/steering-frames.txt` and the whole diagnosis is in its first two rows:

```
frame  mouseAimed  yaw       pos
    0        True    90.000  (0.0506, -0.0080, 1.0800)
    1        True    90.000  (0.0506,  1.0800, 1.0800)
   ...
   39        True    90.000  (1.9734,  1.0800, 1.0800)
```

- **`mouseAimed` is true on every one of the forty frames.** The branch under test is the branch
  that ran.
- **The yaw is exactly 90.000 on every frame.** Nothing rotated the body, so nothing steered it in
  world space.
- **`x` advances by exactly 0.0506 m per step, forty times, due east.** That is
  `Balance.Speed` 4.6 x `AttackerSpeedScale` 0.55 x 1/50 s, to four decimal places. **The steering
  is perfect.**
- **`z` reaches 1.0800 on frame 0 and never moves again.** The entire drift the assertion reported
  happens before a single step of movement.

⚠️⚠️ **IT IS `CharacterController` DEPENETRATION AND THE TEST'S OWN SETUP ASKED FOR IT.** The
controller is left at Unity's defaults (height 2, radius 0.5, centre 0,0,0), so a body placed at
**y = 0.2** has its capsule reaching to **y = -0.8**, and the floor this test builds is a unit cube
scaled 60 x 1 x 60 at y = -0.5, whose top face is at **y = 0**. **The seat starts 0.8 m inside the
ground.** The first `_cc.Move` resolves that overlap, and the shove is not purely vertical: it
lands as (0, +1.08, +1.08).

⚠️⚠️ **AND THE RED'S TWO NUMBERS WERE BOTH CORRECT, WHICH IS WHY IT SURVIVED TWO INVESTIGATIONS.**
`moved.x` is **1.97339928**, which is 39 honest steps: the fortieth was spent on the depenetration.
`moved.magnitude * 0.9` is **2.0246408**, and forty honest steps is **2.0240**. **The bound was
asking for the right answer and the measurement was the right answer minus one frame**, with a
metre of unrelated Z sitting in the magnitude. A red whose numbers are both correct is a red about
the setup.

⚠️ **THE GAME NEVER MEETS THIS, WHICH IS WHY NO PLAYER EVER REPORTED IT.**
`CharacterMotor.Teleport` disables the controller, writes the position, re-enables it and sets
`_spawnSettle = Balance.SpawnSettleFrames`; `FixedUpdate` then PINS the body there for those frames
before any movement runs. **Every spawn in the game goes through it.** This test builds a seat by
hand and skips all of it, which is legitimate and deliberate (its own header explains why it does
not use the arena), as long as the body starts somewhere legal.

- ✅ **The seat stands on the floor now**, at `StandingHeight` y = 1.05: one metre of capsule plus
  five centimetres of daylight, so gravity rather than depenetration puts it down.
- ✅ **A shared `Settle` runs twenty fixed updates before anything is sampled** and asserts the
  body has not drifted sideways, so a future change to gravity or to the controller's size fails
  where the reason is written rather than one assertion further on.
- ✅ ⚠️⚠️ **`AMovementAimedSeatTurnsToFaceItsDirection` WAS CARRYING THE IDENTICAL FAULT AND
  PASSING**, because it only ever asserted a FACING. It is fixed too. **A green test standing in
  the same hole as a red one is the reason to fix the cause rather than the symptom.**
- ⚠️ **THE BOUND IS UNTOUCHED AND SO IS THE ASSERTION**, which is what § 130.14 demanded: *"the
  cause named in `CharacterMotor.Steer` or `CameraRig`, not a widened bound"*. The cause is named
  and it is in neither: it is four characters of Y in the fixture.
- ✅ **`TheSteeringFrameByFrameIsWrittenOut` stays**, on the fixed setup, as the live witness. It
  runs in about three seconds and it is what turns *"the seat ended up in the wrong place"* into
  *"the seat was shoved on frame zero"*.

### 130.17 ✅ EVERY ANDROID REBUILD ON THIS MACHINE FAILED, AND IT IS THE SAME GUARD BEING TOO NARROW FOR THE THIRD TIME

Found by trying to ship the .apk this session's own crossplay fixes need, which is the only way it
could have been found: the FIRST Android build ever made (§ 126.10) wrote into an empty folder and
passed.

```
[Build] output 'C:\Users\Matthew\Desktop\TumbangPreso-Android' exists but does not look like a
previous player (no UnityPlayer.dll, no TumbangPreso_Data, no .app). Refusing to delete it.
```

**`GameBuilder.PurgeOutputDirectory` recognises a previous DESKTOP player and nothing else.** An
Android build writes a single `.apk` plus a `*_BurstDebugInformation_DoNotShip` folder and none of
the three markers it looks for, so from the second build onward the guard refused the folder and
the build aborted. ⚠️ **It aborts AFTER switching build target and running the whole scene check**,
which is several minutes in and reads like a real build failure rather than a path guard.

⚠️⚠️ **THE COMMENT DIRECTLY ABOVE THE TEST ALREADY RECORDS THE macOS INSTANCE OF THIS EXACT
FAULT**: *"BuildMac writes a bundle rather than an .exe beside a _Data folder, so a Windows-only
check refused to purge a perfectly ordinary previous macOS build and failed the build instead."*
**Android is the third platform to meet it and the note did not stop it**, because the note
described one platform's fix rather than the shape of the mistake. The shape is: *this test
enumerates what today's platforms leave behind, so every platform added later fails it once.*

- ✅ **`.apk` and `.aab` join the recognised set.** ⚠️ **The guard's INTENT is unchanged**: it still
  refuses a drive root, a directory holding a `.git`, and anything that does not already look like
  a build. **What widened is the definition of "looks like a build", not the willingness to
  delete**, and `-buildOutput` still takes an arbitrary path from the command line.
- ⚠️ **This is why `CLAUDE.md` § 7's rebuild procedure could not have caught it.** That procedure
  is about a STALE output looking fresh; this is a fresh output that never gets written at all, and
  the .apk on the Desktop kept its old timestamp because nothing had touched it. **Check the
  timestamp, not the exit code**: this run exited **0**.

### 130.18 ✅ § 130.12's ballot, built on a bump somebody else was already paying for

§ 130.12 shipped map rotation over the existing `SelectMap` broadcast **specifically so that it
would not move `ProtocolVersion`**, and said so: *"a feature that did not need the bump did not get
one."* That was right on the day. It also left the vote half unbuilt, with `MapRotationRules.
TallyVote`, the tie-break and `EveryoneHasVoted` written and asserted and nothing on any screen
able to reach them.

⚠️⚠️ **§ 130.13 MOVED THE PROTOCOL TO 22 IN THIS SAME COMMIT, SO THE BALLOT'S WIRE COST WENT TO
ZERO, AND THAT IS THE WHOLE REASON IT WAS BUILT NOW RATHER THAN LATER.** The expensive part of a
new message in this repository has never been the message: it is `CLAUDE.md` § 4a's consequence,
that the Windows player and the .apk must be rebuilt from one commit and shipped together. That
bill was already being paid. **The next chance to add a wire message for free is the next bump,
whenever that is.**

**The ballot is ONE control, and one was a decision rather than a shortcut.** The obvious design is
a button per map. `MatchResult` already carries a result headline, four standings rows, an XP bar,
an ADD FRIENDS list, a rematch vote and two actions, on **the one screen a player sees after every
single match**; three more buttons is § 92's *"theres liek 20 shits at once"* arriving there.
`CLAUDE.md` § 6.2 question 3 is the test, and for two of three map buttons the answer to *"what is
on screen that the player does not need right now"* is always "this one".

- **So it is a chip that names the map the next match will play, and pressing it votes for the next
  map in the list.** A press is a vote AND a preview of what that vote does.
- ⚠️ **THE CHIP READS `Decide` AND NOT `TallyVote`, SO IT STATES A TRUE FACT BEFORE ANYBODY HAS
  VOTED.** With an empty table `Decide` returns the rotation's next map, which is exactly what a
  rematch would load right now. A chip that sat blank until it was used would be a control the
  player has to discover, which is § 6.2 question 2 failed.
- ⚠️ **A PRESS CYCLES THIS SEAT'S BALLOT, NOT THE PROJECTION.** Cycling the projection skips a step
  the moment somebody else's vote changes the answer between two of your presses.
- ⚠️ **THE TALLY LINE IS EMPTY UNTIL SOMEBODY VOTES**, which is `ShowTally`'s own rule one row
  down: a count nobody has contributed to yet is not information, and a permanent "0 VOTES" under
  a chip that already states the answer is a second copy of nothing.
- ⚠️ **A SPECTATOR SEES THE MAP AND CANNOT VOTE.** `_rematch` hides itself from a spectator; this
  does not, deliberately, because a rematch button they cannot press is noise and the next map is
  information they want.
- ⚠️ **IT SITS ABOVE REMATCH.** A ballot underneath the button that consumes it is a control the
  player meets after the decision it feeds has been taken.

**The wire** is `SelectMapVote` (peer to host) and `MapVoteTally` (host to all). ⚠️ **The seat is
resolved on the host from the sender through `TrySenderSeat` and never read out of the payload**: a
client that could name its own seat could cast three ballots and hand itself the map. ⚠️ **The
whole table travels rather than the delta**, for `BroadcastTsinelas`' reason.

⚠️⚠️ **AND THE FALLBACK IS UNCHANGED, WHICH IS THE HALF WORTH RE-READING § 130.12 FOR.** The host
still calls `AdvanceMapRotation` on every rematch and the ballot is only ever an ARGUMENT to it.
There is no branch for "somebody voted": `Decide` reads an empty table and returns the cycle. **A
lobby where nobody presses anything is the common case**, and the vote alone would have left four
abstentions replaying the same street for ever.

⚠️ **NOT DONE: `MapRotationRules.VoteSeconds` (20) IS STILL UNUSED.** The board has no countdown,
so the ballot is open for as long as the board is up rather than for a fixed window. That matters
only for a lobby where somebody walks away mid-vote, the rematch already has its own tally gate,
and a second clock on this card needs the design pass § 6.2a asks for rather than a number.

⚠️⚠️ **AND NOBODY HAS SEEN THIS CARD IN A BUILD.** `CLAUDE.md` § 6.2b: a screen with a new row
ships with a render, over the real background, at the shape he plays at, with the chrome live.
**This one has a test and no picture**, which is § 6.2a's *"a green layout probe is not a good
screen"* with the probe not yet run either. It is the first thing to look at in the next build.

---

### 130.16 · Verified

- **Core 451/451** (`dotnet test`, 44 ms). 433 plus 2 pool-key tests plus 16 `MapRotationTests`.
- **EditMode 305/305** (4 s). 297 plus 4 `NetIdentity` retry tests plus 4
  `WorldCameraPassParityTests`.
- **All seven editor checks pass in one launch**: headless, arena, map geometry, audio cues, scene
  scripts, input surface, and the new shader warmup. ⚠️ The last one REGENERATES rather than
  inspecting, which is why it is a check rather than a test.
- **`[Build] SUCCEEDED. 770 MB, 52s`** to `C:\Users\Matthew\Desktop\TumbangPreso-Unity\TumbangPreso.exe`,
  timestamps from this run. The build log carries `[ShaderWarmup] 23 shaders, 53 variants` and
  **`0 Shader.Find name(s) did not resolve`**, so the collection in the shipped player is the
  regenerated one rather than a stale asset.
- ⚠️ **`NetSession.ProtocolVersion` is untouched at 21**, read from the file. Nothing in this batch
  went near the wire: the map rotation rides the existing `SelectMap` broadcast and the pool key is
  not a wire message, it is a string two peers compare.

**Targeted PlayMode, the seven suites this batch touched: 11 of 13.** ⚠️ Targeted rather than full,
which is § 126.8's own instruction until that entry closes.

| | |
|---|---|
| `SteeringTests.MouseAimedMovementIsRelativeToTheBody` | **§ 130.14. Pre-existing, deterministic, measured at HEAD without this batch's teardown and identical to eight significant figures** |
| `AspectRatioProbes.TheCharacterScreenSurvivesEveryAspectRatio` | **§ 130.15. `PaperKit.Caption` at 16, which is § 121.8's open design question** |

⚠️⚠️ **AND THE HONEST LIMIT ON THE § 126.8 CLAIM: THE FULL SUITE HAS NOT BEEN RUN ON THIS COMMIT.**
The cause is identified and the five named fixtures are fixed, but *"the suite is a gate again"* is
a claim only a full run can make, and quoting one would be the exact fault § 126.8 exists to record.
**Do not close § 126.8 on this entry.**

### 130.19 · Verified, the second batch on this branch

⚠️ **The block above is the FIRST batch's numbers and is left alone.** This one is the commit that
moved the protocol.

- **Core 454/454** (`dotnet test`, 48 ms). 451 plus `ARoundIsDecidedByOneSurvivorOrByNoneAndNeverByTwo`,
  `TheLastStandingAwardIsAKnockdownsWorthAndIsReadFromOnePlace` and
  `TheScoreEventOrdinalsAreAWireContractAndOnlyGrowAtTheEnd`.
- **EditMode 316/316**. 305 plus 8 `LastTsinelasMatchHalfTests` and 3 `MapBallotWiringTests`.
  ⚠️ Two of the eight are the client-only defects above and were written after the first build,
  which is why this number is 316 and not the 314 the first pass reported.
- **All seven editor checks pass in one launch**, and `[ShaderWarmup] 23 shaders, 53 variants`
  with **0 `Shader.Find` names unresolved**, unchanged by this batch.
- **All three `tools/` audits exit 0**: 49 ability effect sites with **0 ungated on another body**,
  **57 wire entry points and 0 unreachable** (the two new ballot messages both have callers), and
  **60 named messages with 0 mismatched** (`Tsinelas` reads 3 and writes 3).
- **`[Build] SUCCEEDED. 770 MB, 66s`** to `C:\Users\Matthew\Desktop\TumbangPreso-Unity\TumbangPreso.exe`
  and **`[Build] SUCCEEDED. 1900 MB, 154s`** to
  `C:\Users\Matthew\Desktop\TumbangPreso-Android\TumbangPreso.apk`, **both from `bb72232`**, four
  minutes apart, timestamps from those runs. `[ShaderWarmup] 23 shaders, 53 variants` with **0
  unresolved `Shader.Find` names** regenerated inside the build.
- ⚠️⚠️ **SHIPPING THEM TOGETHER IS THE POINT AND NOT A FORMALITY AT THIS COMMIT**, because
  `ProtocolVersion` moved: a v21 phone and a v22 PC refuse each other correctly and it reads as a
  bug (`CLAUDE.md` § 4a).
- ⚠️ § 130.17 is why the `.apk` timestamp is quoted rather than the exit code:
  `PurgeOutputDirectory` aborted every Android rebuild for weeks, silently, at exit 0 with an
  unchanged timestamp. **The purge ran and the old file was gone before IL2CPP started**, checked
  by hand this time.

⚠️⚠️ **`NetSession.ProtocolVersion` IS 22 AND THAT IS THE HEADLINE OF THIS COMMIT.** § 130.13 is
why. **Both players were rebuilt from this commit and shipped together**, per `CLAUDE.md` § 4a: a
v21 player and a v22 player refuse each other correctly and it reads as a bug.

⚠️ **One test of mine failed first and the failure was the test, not the code.**
`TheAwardIsCreatedWhereEveryOtherPointIsAndBeforeTheBufferOpens` searched for the bare string
`BeginIntermission` to prove the award is written before it, and found it in `Decide`'s own doc
comment several hundred characters ahead of the award. **Every ⚠️ note in this repository names the
thing it is about, so in this codebase a bare identifier is nearly always a comment before it is
ever a call.** The test matches `GameServices.Match?.BeginIntermission()` now. Worth remembering
before writing the next source-as-text assertion.

⚠️⚠️ **THE FULL PLAYMODE SUITE STILL HAS NOT BEEN RUN ON THIS COMMIT AND § 126.8 IS STILL NOT
CLOSED.** Verification here was `dotnet test`, EditMode, the seven checks, the three audits and a
targeted `GameplayShots` run for § 127.3's frame. **Do not quote a PlayMode number from this
entry.**

### 130.15 ⚠️ `AspectRatioProbes` IS NARROWED TO ONE OPEN QUESTION NOW, AND IT IS § 121.8

After § 130.11's three fixes the character screen's only remaining red is **`DoorCaption`, authored
at 16**, which is exactly `PaperKit.Caption`. That is not an unnoticed bug: § 121.8 is the entry
holding it open, `PaperKit`'s own header states the 16-against-18 conflict as a deliberate decision,
and that entry says in as many words that it is **"settled by looking at the running build and not
by either file winning on paper"**.

- ⚠️ **THE FLOOR IS NOT LOWERED AND THE CAPTION IS NOT EXEMPTED.** § 126.13: *"Do not lower the
  probe's floor to make it green."* Raising `PaperKit.Caption` grows **every caption in the front
  end** by an eighth, and § 121.8's method is to walk the seven screens that use one and split them
  into "a caption that restates a value above it" and "a caption that is the only place a fact
  appears" **before** touching the constant.
- ✅ **What did change is that the probe now NAMES § 121.8 in its failure message**, so the next
  reader is told this is an open design question rather than re-deriving it from two constants.
  **The probe has gone from "some label somewhere on this screen is small" to one constant and one
  entry**, which is the useful half of narrowing a permanent red.
- ⚠️ **It is still red and should stay red until § 121.8 is settled**, which needs 🧑's eye on a
  running build. `CLAUDE.md` § 4a: *"a green probe for a screen nobody can reach is worse than a red
  one"*, and the inverse holds here — a red that names its own open entry is doing its job.


## 128 · Phases 11 and 12 are almost entirely built, and this entry was wrong about it once ⚠️ OPEN, 2026-09-03, branch `ui-redesign`

Checked against the code rather than the plan, which is `docs/FUTURE.md` § 0.5 rule 2.

⚠️⚠️ **AND THE FIRST VERSION OF THIS SECTION, PUSHED IN `c89d646`, SAID THE RATING DID NOT READ
`IsBot`. IT DOES. THE MISTAKE IS KEPT HERE BECAUSE OF HOW IT WAS MADE.** I grepped
`Packages/.../Rating*.cs` and `MatchRecord.cs` for the word "bot", found the FIELD and no use of
it, and wrote the conclusion. **The use is in two places that grep could not see**: it is spelled
`humans` rather than `bot` on the C# side (`RatingRules.Blend(before, after, weight)`), and the
caller is not C# at all, it is `ugs/cloud-code/match-record.js`:

```js
const humans = record.Players.filter(p => p && !p.IsBot).length;
const weight = botWeight(humans, record.Players.length);
const after  = blendRank(profile.Rank, ..., weight);
```

**A grep for the noun missed the code that does the thing.** `CLAUDE.md` § 4a's whole argument is
that a rule kept by remembering goes stale; this is the reader's version of it, and the lesson is
`audit_request_call_sites.py`'s in miniature: **ask what calls it, not what mentions it.**

### 128.1 What is actually built, so nobody rebuilds it

- ✅ **Bot difficulty tiers**, a NONE option, and `AiTuning.For(tier)`. `MatchInstaller` applies it.
- ✅ **Bots labelled `BOT`** in the lobby, from `BotFillRules.BotTag`, one string in the core.
- ✅ **Bot fill after a wait threshold.** `BotFillRules.CasualFillAfterSeconds` is **45**, ranked is
  **150**, and `Matchmaker` and `QueueCard` both call it. Phase 11's *"a 45-second queue that ends
  in a playable match beats a 4-minute queue that ends in nothing"* is the shipped number.
- ✅ **The rating knows.** `BotFillRules.Weight` is a straight line (every human seat past the
  first is a quarter of the result), `RatingRules.Blend` scales **rating, deviation AND volatility**
  so a bot farm cannot buy confidence cheaply, a zero-weight match does not count as a season
  match, and the same table is written again in `match-record.js` because the server computes the
  rating and the game has to be able to predict it. `Phase11And12Tests` asserts both sides.
- ✅ **MIRROR**, including `CustomGame.MirrorIndex` picking the same character on every machine
  from the week number with no service at all, and a test for the pre-epoch clock a venue produces.
- ✅ **Custom games**: password, rounds, round length, bots, formats, the wire parse, and
  `CanBeRanked` refusing every one of them.

### 128.2 ⚠️ What is genuinely open in Phase 12

- ⚠️⚠️ **LAST TSINELAS STANDING HAS RULES, TESTS, A DOCUMENT AND NO MATCH HALF.**
  `CustomGameRules` carries `StartingTsinelas` 3, `TsinelasLeft`, `IsOut` and
  `LastAttackerStanding`, and `Phase11And12Tests` asserts all of them. **`MatchFormat.LastTsinelas`
  appears nowhere in `Assets/TumbangPreso/Runtime/`**: `MatchInstaller` handles `Mirror` and
  nothing handles this. It is also deliberately absent from the lobby's format list
  (`ConvertedMatchSetup.FormatAt` maps index 0 to Standard and anything else to Mirror), which is
  correct: **a format a player can pick and the match cannot run is worse than one that is not
  offered.** § 129 is the match half.
- ✅ **Map rotation and a map vote are BUILT, 2026-09-03. § 130.12 and `docs/Formats.md` § 4.**
  `MapRotationRules` is engine-free with sixteen tests, and a REMATCH now moves to the next map,
  host-only, over the `SelectMap` broadcast that already exists — **so the protocol did not move**,
  which mattered because moving it forces the Windows player and the .apk to be rebuilt together.
  ⚠️ **The BALLOT is not wired across the wire yet**: `Decide` takes votes when a lobby has some to
  give it, and until then silence falls through to the cycle. Collecting votes is a new message and
  therefore a protocol move, and it should be spent in the same bump as the match half below rather
  than on its own.

- ⚠️⚠️ **AND WHAT THE MATCH HALF ACTUALLY COSTS IS § 130.13, WHICH THIS ENTRY DID NOT KNOW.** A
  Last Tsinelas round ends EARLY and awards points nobody else can compute, so it needs a new
  `ScoreEvent` and it moves `ProtocolVersion` to 22. **21 covers "the peer knows about
  `MatchFormat`", not "the peer knows how to run this one"**, and those are different claims.

---
## 127 · Phase 16.1: the taya is a RING and an attacker is a DISC ⚠️⚠️ OPEN, 2026-09-03, branch `ui-redesign`

`docs/FUTURE.md` § 16.1 is the one item the handoff into this session called out as *"real"*:

> `Art_Direction.md` § 1 is a law that never bends: **orange is OFFENSE, blue is DEFENCE**, and the
> whole HUD, every role marker and every readability decision rests on it. ... **the roles are
> currently distinguished by hue alone**. A colourblind player, a bad projector at a tournament, or
> a cheap phone screen all produce the same failure: you cannot tell the taya from the attackers.

### 127.1 Half of that claim was already stale, and the half that was not is the one that matters

⚠️ **§ 0.6's own instruction is to check a claim before acting on it**, and checking this one moved
the work. **Two surfaces already carry the role in a second channel:**

- **The scoreboard.** `Hud`'s row builder prints `DEFENDER` or `ATTACKER` as a WORD in every row and
  spends the role colour on that cell alone, deliberately: *"that is the one cell where the colour
  IS the content."* The role rail and the row plate carry it again at a distance.
- **The floating tag over a unit.** `CharacterNameplate` writes `NAME · TAYA` on the defender and
  the bare name on the other three, and its note explains that the suffix is on the taya alone
  because *"which of the four is the taya is the one fact worth naming in the world"*.

⚠️⚠️ **THE FLOOR RING WAS THE ONE THAT WAS STILL HUE-ONLY, AND IT IS THE ONE A PLAYER ACTUALLY
READS.** One disc per unit, same radius, same shape, `UiTheme.Defense` or `UiTheme.Offense`. It is
on the floor where the retrieval happens, and the tag above the head fades out past `FadeStart`,
**twelve metres**, in a box that is fourteen metres across. At range, in a fight, the ring is the
whole signal.

### 127.2 A shape, not a second colour, and it costs less floor rather than more

**The taya's ring is a `VfxShapes.Collar` (an annulus) and an attacker's stays the filled disc.**

- ⚠️ **`CLAUDE.md` § 6.5 already states this rule one subsystem over**: *"a chamfer means pressable
  and a round means furniture ... a shape difference survives a photograph and a colourblind
  player; a fill difference does not."* This is that rule applied to the floor.
- ⚠️⚠️ **AND IT SPENDS LESS OF `docs/VISION.md` § 2'S AREA BUDGET, WHICH `Collar` ARGUES ITSELF:**
  *"a ring at 8 per cent thickness costs about a sixth of the painted floor its filled equivalent
  does, at the same radius, carrying the same information about where the edge is."* The accessible
  answer is also the cheaper one, which is not usually how this goes.
- ⚠️ **IT REUSES A GENERATOR RATHER THAN ADDING A NINTH.** `VISION.md` § 2 rule 3: *"five polygons
  handed to one builder are one thing."* `Wedges` was the other candidate and is wrong here for a
  reason worth writing down: **it jitters every plate on purpose**, and a role marker has to be the
  same shape on every taya in every round or it is not a signal.
- ⚠️ **ONLY THE TAYA CHANGES**, which is the decision the tag above it had already made. There is
  exactly one taya and everybody else is an attacker by definition.
- ⚠️ **The scale had to become role-dependent and that is the one place this can go wrong.** A Unity
  cylinder primitive is built at radius 0.5 and `Collar` at radius 1.0, so `_ringUnitSpan` carries
  the factor and `Refresh` sets it in the same breath as the mesh. Set one without the other and
  the marker draws at twice or half its size.

### 127.3 ⚠️ OPEN: what is left of Phase 16

✅ **The shape is proven as GEOMETRY, and that is not the same as proven as a picture.**
`Assets/TumbangPreso/Tests/RoleMarkerTests.cs`, EditMode, two cases: the taya's mesh has **no
vertex nearer than 0.594 of its own radius** (so there is a real hole in it) and is built at unit
radius, and the attacker's cylinder reaches its own centre (so the two are not the same shape).
**EditMode is 297/297 with them in.**

⚠️⚠️ **AND ASSERTING THE MESH IS WHAT A RENDER COULD NOT DO HERE, WHICH IS WORTH WRITING DOWN
BECAUSE `CLAUDE.md` § 6.1 SAYS TAKE THE PICTURE.** The picture was taken: `GameplayShots` ran a
live round and `Logs/shots-play/round-witness.png` shows an attacker's orange disc cleanly from
above. **The taya's marker was caught only edge-on**, from a first-person camera standing on top of
it, and from that angle a ring and a disc are the same picture. The camera goes where the round
puts it. **A hole in a mesh is a fact about the mesh**, so it is asserted where a camera angle
cannot defeat it.

- ⚠️⚠️ **STILL OWED: THE GREYSCALE FRAME, AT AN ANGLE THAT SHOWS THE TAYA'S FEET.** *"If the taya
  cannot be picked out of a desaturated frame, the second channel is not there yet."* The mesh test
  says the code did what it was told; **it does not say a player can read it at eight metres.**
  `scratchpad/greyscale.py` desaturates at Rec. 601, the same weighting `AbilityShowcaseProbe`
  measures luminance with. **Do not close § 127 without it.**

  ⚠️⚠️ **AND `scratchpad/greyscale.py` DID NOT EXIST WHEN THIS ENTRY NAMED IT, WHICH IS WORTH
  RECORDING RATHER THAN QUIETLY FIXING.** The line above was written as though the tool were on
  disk and it was not, so every session that read this entry was told the hard half was already
  done and only the render was owed. **A pointer to a file that is not there is worse than no
  pointer**, because it makes the work look smaller than it is: this is the same fault class as
  § 96's probe asserting a plate was on screen, one level down. Written 2026-09-03, Rec. 601, and
  it refuses to overwrite an existing output because `CLAUDE.md` § 6.1 versions render filenames.

  ⚠️⚠️ **AND `round-witness.png` COULD NEVER HAVE ANSWERED THIS, FOR A REASON WORTH NAMING: THE
  CAMERA IS AT THE WRONG HEIGHT, NOT IN THE WRONG PLACE.** `GameplayShots.Witness` sits 2.6 m up
  and looks at 0.9 m, roughly chest height on a standing Person, which frames the CAST. A role
  marker is painted on the FLOOR, and from a near-level camera a ring and a disc are the same
  picture. `GameplayShots.RoleMarkers` is a second camera: 8 m away on the ground plane (§ 16.1's
  own distance, not whatever framed nicely), 4.2 m up, aimed at the taya's FEET, and placed on the
  line between the taya and the nearest attacker **so both markers are in one shot at similar
  sizes**. A frame with one marker in it cannot fail the test, because the claim is not "the ring
  is visible", it is "the taya can be picked out".

  **Taken 2026-09-03**: `Logs/shots-play/role-markers-v1.png` and `role-markers-v1-grey.png`.
  ⚠️⚠️ **STILL OPEN, BECAUSE THE ANSWER IS A JUDGEMENT AND IT NEEDS 🧑'S EYE**, which was always
  true of this item. What the frame adds is that the question is now one LOOK rather than one task.
  Two things were measured off it and both are worth having before that look:

  - ⚠️⚠️ **THE RING IS DRAWN AND IT IS FAINT. 1,909 blue-dominant pixels in a 1920x1080 frame**,
    at rows 304 to 730 peaking at **row 563**, which is the taya's feet. So the mesh reaches the
    screen and § 127.3's geometry test was not lying. **In the desaturated frame it does not
    separate cleanly**: `UiTheme.Defense` `0080e8` is Rec. 601 luminance **101** and Eskinita's
    asphalt sits near 60, so the ring is about 40 levels of grey over its background, spread over
    an 8 per cent-thick annulus at eight metres. The SHAPE is the second channel and there is not
    much of it to see. **That is the thing to judge in the picture.**
  - ⚠️⚠️ **AND A TRAP FOR WHOEVER TAKES THIS PICTURE NEXT: `CharacterNameplate` SWITCHES THE RING
    OFF ENTIRELY FOR THE LOCAL FIRST-PERSON BODY** (`_ring.gameObject.SetActive(!mine)`, guarded by
    `rig.IsLocalFpp && rig.IsFollowing`). It is correct behaviour and it is a floor for a marker
    nobody can see from inside their own head. **But it means a witness camera framing the local
    player photographs a body with no marker at all**, and the obvious reading of that frame is
    "the ring is not being drawn" rather than "the ring is deliberately hidden on this one body".
    That is half an hour of chasing a bug that is not there.

  ⚠️ **The measurement was made by counting pixels rather than by looking**, because "I cannot see
  it" and "it is not there" are different findings and only one of them is actionable.
- ⚠️ **The crosshair and the lata label are still hue-only** (`Hud`: `UiTheme.Offense` /
  `UiTheme.Defense`). Both are small and the lata card has words beside it, so they are a lower
  bar than the ring was, but they are the rest of § 16.1.
- ❌ **Nothing else in § 16.2 is started**: UI scale, larger text, hold-versus-toggle for sprint and
  grab, FOV and shake sliders, a reduced-effects mode, subtitles for callouts, a high-contrast HUD,
  flash reduction, and colour-independent slipper highlights. ⚠️ **`AbilityShowcaseProbe`'s 12 per
  cent screen-white bound already half implements the flash item**, which is the cheapest of them
  to finish.
- ⚠️ **§ 16.3 (localisation) is CUT and must stay cut.** 🧑: *"english only"*. The cost was never
  the translation, it was keeping three languages in step for ever.

---

## 88 · Accounts and identity ⚠️ IN PROGRESS 2026-08-31

Phase 1 of `FUTURE.md` was explicitly commissioned. Its preflight was stale in an important way:
Authentication was not unused. `NetIdentity` already signed in anonymously at boot, cached the
attempt, persisted the UGS session, and degraded to a local token for LAN. `UgsCheck` also exercised
that path. The account layer extends that proven seam rather than creating a second sign-in owner.

**Built in this phase:** `PlayerAccount` owns the player id, display name, discriminator, bio,
country, pronouns and signed-in/local state and raises `Changed`; the splash awaits its bounded
boot barrier before activating the menu; username/password linking uses
`AddUsernamePasswordAsync` and asserts the anonymous PlayerId did not change; username sign-in can
move the account to a second device; deletion clears Cloud Save before deleting Authentication;
the first score queues the upgrade offer for the next menu; and offline tournament guests get an
ephemeral local identity without overwriting the machine owner's account.

**Database shape:** Authentication owns credentials and the stable id. The server-side Cloud Code
script `ugs/cloud-code/player-account.js` validates profile writes and stores one protected
`accountProfile` record in Cloud Save. The client never writes Cloud Save directly. Local JSON is
the offline cache and remote valid fields win when the endpoint answers.

**Lobby identity:** every local hello, identify message and beacon reads `PlayerAccount.LobbyName`
rather than `Settings.PlayerName`; the host accepts only a core-validated `display#1234` handle and
allocates a deterministic fallback tag for an invalid claim.

**Done looks like:** the Phase 1 acceptance list in `FUTURE.md` § 1 plus a deployed
`player-account` Cloud Code endpoint, Core, EditMode and focused PlayMode green, an unplugged LAN
run, a clean Windows player on the Desktop, committed and pushed.

### 88.1 · Three things the first pass got wrong, found by running its own tests

The account work above was written but never committed and never run: the session that wrote it
went looking for a Unity package-manager fault instead. There was none. `Temp/UnityLockfile` was
stale, which is § 7 of `CLAUDE.md` verbatim, and after removing it the project compiles and the
suites run. **Nothing was wrong with the editor, the `Library`, or the machine's UPM cache.**
`AppData\Local\Unity\cache\upm.accounts-backup` is that session's moved-aside copy and can be
deleted; Unity rebuilt `upm` beside it.

Running the suites then found three real defects, all in the new code.

**88.1a · Two name lengths, and the shorter one is on the wire.** `AccountRules.DisplayNameMax`
was written as 16 while `Balance.PlayerNameMax` has been 14. That is not a cosmetic
disagreement. `LanBeacon` truncates the name it broadcasts to the `Balance` value, the settings
field sets `characterLimit` from it, and `Hud`'s row width was measured against that many "W"s
(`Hud.cs` ~2895). A 16-character account name therefore renders past a measured layout and
arrives over LAN clipped, so the name in the profile and the name on the scoreboard stop being
the same string. `DisplayNameMax` is now `= Balance.PlayerNameMax`, one constant, and
`TheAccountNameLimitIsTheOneTheWireAndTheHudUse` fails if they are ever split again.
⚠️ The Cloud Code validator had the same 16 hardcoded, and the server is the authority: it would
have stored a 15-character name that every client then silently clipped. It reads 14 now, with a
comment saying it is the one place the number is written twice.

**88.1b · Every LAN peer was renamed `Player#tag`, which is the nationals case.** Arrival kept
the claimed name only when it was already a full `name#1234` handle and rewrote everything else to
`Player#tag`. Everything else is: every LAN peer, every build older than this branch, and every
client whose profile has not finished loading. Four machines joining off the beacon in a hall
would have rendered as four rows nobody could tell apart, in the one venue where that matters
most. `AccountRules.ArrivalHandle` now keeps a usable claimed name and allocates the tag from the
durable token, and falls back to `Player` only for a name that cannot be shown at all. It sits in
the core rather than in `LobbySession` because it is a rule, per `FUTURE.md` § 0.5 rule 3.

**88.1c · ✅ CLOSED 2026-08-30 BY § 90.1, WHICH ALSO CORRECTS THE FIX THIS ENTRY PRESCRIBES.**
⚠️ The last paragraph below says to have the host ask the endpoint whether a player id owns a
handle. **That does not work on its own**: the player id arrives in the same message as the
handle, from the same peer, so an impostor claims both and the endpoint truthfully says yes. The
missing half is proof that the peer IS the account it names. § 90.1 has what shipped. The
statement of the problem below is still exactly right and is kept for it.

**The problem, as first written:** ⚠️⚠️ **THE IMPERSONATION GUARD DOES NOT EXIST YET, AND THE
TEST SAID IT DID.** The
brief's reason for routing the lobby name through the account was that *"the first thing anybody
does with a new account system is impersonate somebody"*. The rule written for it was backwards.
It rewrote a bare `Maria Clara` on the theory that it was forging `Maria Clara#4417`, while
admitting a claimed `Maria Clara#4417` **verbatim** in the assertion directly above it. So it
punished the honest case and waved the actual attack through, and
`LobbyAcceptsOnlyCanonicalAccountHandles` asserted that as correct behaviour.

**A peer-hosted lobby cannot close this on its own.** The tag of a real account is allocated by
UGS Player Names, so the host cannot recompute it from the token and cannot tell a genuine
`Maria Clara#4417` from a claimed one. Closing it needs the host to ask the `player-account`
endpoint whether this player id owns this handle, cache the answer, and **fall through to the
claim on LAN or when the endpoint is unreachable**, because § 0.5 rule 7 says a LAN match may
never sit behind a login. That is the next piece of work on this phase and it is not built.
The test is now `LobbyKeepsAUsableNameAndAlwaysTagsIt` and asserts only what is true today.

**88.1d · The splash could wait forever on a service that answers slowly rather than not at
all.** The splash holds the menu until `PlayerAccount.InitializeAsync` completes, and the splash's
own `MaxWait` only logs a warning: its loop has no upper bound. The boot budget raced **only the
sign-in**, leaving `RefreshFromAuthenticationAsync` unbounded behind it, and that awaits Player
Names and then Cloud Save. A service that accepts the connection and then never answers is exactly
what venue Wi-Fi behind a captive portal does, which is the network the nationals will be played
on, and it is the one failure a try/catch cannot see. The budget now covers the whole remote path.
⚠️ A late answer is not thrown away: the work keeps running and applies itself through `Changed`,
so a slow connection costs a few seconds of showing the local name rather than the account.

**88.1e · A tournament guest overwrote the machine owner's account.** `SignInAsGuest` documents
that it does not replace the owner's account, and `Persist` broke that promise: every write goes
through `Apply`, so a guest editing a profile wrote the guest id, name, tag and bio straight over
the owner's saved account. That is somebody handing their laptop over for one match at an offline
tournament and getting it back with a different account on it. `Persist` now returns early while a
guest is active, a guest is refused `DeleteAsync` outright (it clears the settings file, and the
settings file is still the owner's), and a late remote answer parks itself as what `LeaveGuest`
returns to rather than applying over the guest mid-session.

**88.1f · The upgrade offer wrote `settings.json` on every point scored.** `MarkWorthKeeping` is
reached from `MatchDirector.AddScore`, which is every point, and passive defence pays +10 a second
while the lata stands. It set the pending flag and saved without first checking whether the flag
was already set, so a round reserialised the settings file about once a second per defender, on
the thread the match steps on. The decision is now `AccountRules.ShouldQueueUpgradeOffer` in the
core, with the already-pending term as a named argument and three tests on it, so it is one write
per session.

**Verified:** Core 135/135 (`dotnet test`), from 69 before the phase. EditMode 236/236, read from
`Logs/tests.xml` rather than the exit code.

### 88.2 · The UGS project belongs to somebody else, and the service half is blocked on that

🧑, 2026-08-31, shown the dashboard steps: *"can we js connect it to my acct instead bcz this
isnt mine"*.

**The project is `090f4720-e3f8-466f-b8f5-7679c6b41fb1` under org `paulandreirecio22`.** The Unity
Hub on this machine is signed in as `M4tyuuu` / `matthewtlabrador@gmail.com`, whose own org is
`matthewtlabrador`. So the editor is his and the cloud project is not, which is why he cannot make
the service account that Cloud Code deployment needs.

⚠️ **THE LINK IS TWO LINES AND NO CODE.** `cloudProjectId` and `organizationId` in
`ProjectSettings/ProjectSettings.asset`, lines 738 and 742. Nothing in `Assets/`,
`Packages/`, `ugs/` or `docs/` holds a copy; `PlayerAccount.CallCloudAsync` reads
`Application.cloudProjectId` at runtime and follows the file. `tools/relink_ugs_project.sh` does
the swap, refuses an id that is not a UUID, and prints what it cannot prove.

**Three things to know before relinking, none of which are reasons not to:**

1. ⚠️ **A UGS PlayerId is scoped to its project, so this is a reset rather than a transfer.** Every
   existing anonymous id dies. **That costs nothing today and will not stay that way**: once
   Phase 2 gives people profiles, stats and match history, the same move throws all of it away.
   **Now is the cheapest this will ever be.**
2. ⚠️⚠️ **EVERY MACHINE MUST BE ON THE SAME PROJECT OR ONLINE PLAY SILENTLY STOPS WORKING BETWEEN
   THEM.** Two builds on different UGS projects resolve a join code in different namespaces, so
   the room is simply not there. It does not read as a misconfiguration, it reads as an empty
   lobby. His second laptop and any teammate's build must be rebuilt off the same branch.
   **LAN discovery is unaffected**, because `LanBeacon` never touches UGS.
3. The new project needs Authentication (anonymous **and** username/password), Relay, Lobby,
   Cloud Save and Cloud Code enabled. Relay and Lobby are already proven on the old project, so
   the shapes are known good; only the toggles move.

⚠️⚠️ **AND `UgsCheck` CANNOT VERIFY ANY OF THIS HEADLESSLY, WHICH COST A RUN TO LEARN.**
`UnityServices.InitializeAsync` refuses outside Play Mode with *"Unity Services can only be
initialized in Play Mode"*, so a batchmode `UgsCheck.Run` reports step 2 and then FAILS step 3 for
a reason that has nothing to do with the project. Batchmode also has no Hub session token, so it
cannot see the signed-in account either, which `UgsCheck`'s own step 1 comment already says.
**The real check is the menu item `Tumbang Preso > Check UGS Wiring` from an open editor.**
⚠️ Also do not pass `-quit` alongside `-executeMethod UgsCheck.Run`: it polls from
`EditorApplication.update` and exits itself, so `-quit` kills it before one UGS call is pumped and
the log just stops after the compile.

**✅ RELINKED 2026-08-31.** The project is now
**`dcf0831e-a5f4-43b4-832e-b687f13a3569`** under org **`matthewtlabrador`**, genesis org
`18968483660152`, created by 🧑 on his own account. `ProjectSettings.asset` lines 738 and 742 are
the whole change and `git diff --numstat` on that file reads `2 2`.

**Verified so far:** `OnlineSignInProbe` passes 2/2 against the new project, including
`ALinkedBuildNeverSettlesOnNotLinked`, so the build resolves the new id rather than reporting
itself unlinked. ⚠️ **That is not proof the services are on.** The probe asserts the boot attempt
happens and settles; it passes offline too. Relay and Lobby are only provable from
`Tumbang Preso > Check UGS Wiring` in an open editor.

⚠️⚠️ **PAUL'S BUILD CANNOT SEE THIS ONE ONLINE UNTIL HE REBUILDS OFF THIS BRANCH.** Different UGS
project means a join code resolves in a different namespace, so the room is not there and it reads
as an empty lobby rather than as an error. **LAN is unaffected.** This is the first thing to check
if anybody reports online play "broken" after pulling.

⚠️ **COPPA on the new project reads "NOT primarily targeting children" and must stay that way.**
Marking it child-directed disables username/password auth and restricts Cloud Save, which is
exactly the account layer § 88 just built.

### 88.3 · What the new project actually needed, which was one click and not five

⚠️⚠️ **RELAY, LOBBY AND CLOUD SAVE HAVE NO ENABLE TOGGLE ANY MORE. THEY AUTO-PROVISION ON FIRST
USE.** Four of the five "services to switch on" written above did not exist as switches. Their
dashboard pages are documentation with no enable button, and a fresh project answers Relay and
Lobby calls immediately. Time was spent hunting toggles that were never there.

⚠️ **AND ANONYMOUS SIGN-IN IS NOT AN IDENTITY PROVIDER.** The Identity Providers page reading
*"You have no identity providers"* is the correct healthy state for a project whose anonymous
sign-in works, and it was briefly read as a misconfiguration. **The one thing that genuinely had to
be added was Username & Password**, which IS a provider, and which the account upgrade path needs.
It is added and Enabled as of 2026-08-31.

✅ **Proven live on `dcf0831e-a5f4-43b4-832e-b687f13a3569`**, `UgsServicesProbe` 3/3:
anonymous sign-in returned `qmSg3PKweGRSWqRcd9g0Bo80UKH4`, Relay allocated join code `WGF96G` for
a host of three, and a private lobby was created and deleted.

**`Assets/TumbangPreso/Tests/PlayMode/UgsServicesProbe.cs` is new and is the answer to the
`UgsCheck` problem in § 88.2.** Play Mode is the one context allowed to initialise UGS, so the
three calls that a batch `UgsCheck` cannot make are made from there instead. ⚠️ It is
`[Category("Ugs")]` and excluded from the default PlayMode run, for the same reason `WallClock`
excludes `AiDiagnosticProbe`: it talks to a live service, needs a network and spends free-tier
quota. Run it on purpose after a relink:

```
Unity.exe -batchmode -runTests -projectPath . -testPlatform PlayMode           -testCategory "Ugs" -testResults Logs/ugsprobe.xml -logFile Logs/ugsprobe.log
```

⚠️ **`OnlineSignInProbe` passing is not the same claim.** It asserts the boot attempt happens and
settles, which is true offline and true against a project with every service off. It answers "did
we try"; this answers "did the service say yes". Both are wanted.

### 88.4 · ✅ The service half is done. Phase 1 is complete.

Service account `tumbangpreso-deploy` exists, `ugs login` is stored locally, and
`ugs deploy ugs/cloud-code` created **`player-account`**, which reports `script is already
active` on a publish attempt because deploy publishes it.

✅ **`UgsServicesProbe` 4/4**, and the fourth is the one that matters: `TheAccountEndpointAnswersALoad`
calls the live endpoint with a real player bearer token and gets `{"output":{"profile":""}}`.
An empty profile is the correct answer for a player who has never saved one. That single test
proves the deploy, the publish, the service-account roles and the client's auth all line up,
which is every part of this that could be misconfigured.

⚠️ **THE CLI NEEDS MORE THAN CLOUD CODE EDITOR AND CLOUD SAVE EDITOR, WHICH COST A ROUND TRIP.**
With only those two, every command fails `403 Forbidden` on **`GetEnvironments`** rather than on
the thing being asked for, because the CLI resolves the environment before it does anything else.
The error names an endpoint nobody asked for, which is what makes it confusing. 🧑 resolved it by
granting the service account everything. ⚠️ **If that is ever tightened, keep an environments read
role**, or the CLI breaks again with an error that does not mention environments.

❌ **SUPERSEDED 2026-08-30, AND THE PARAGRAPH BELOW IS KEPT BECAUSE ITS REASONING IS WHY.**
The duplicate is gone: Phase 2 needed a THIRD copy of this request, so it moved into
`Assets/TumbangPreso/Runtime/Net/CloudCode.cs`, which the game itself calls, and the probe now
calls that. A shared helper the shipping code uses is not the seam this paragraph was refusing,
and `CareerAndCloudCodeTests.EveryCloudCodeRequestGoesThroughTheOneHelper` fails if a fourth is
ever written by hand. § 89.5.

⚠️ **`TheAccountEndpointAnswersALoad` DUPLICATES `PlayerAccount.CallCloudAsync` ON PURPOSE AND THE
TWO MUST MOVE TOGETHER.** That method is private, and widening it so a test could reach it would
put a seam in shipping code for one probe. The duplication is the lesser cost, but if the call
shape drifts, **the probe passes while the game fails**, which is the worst outcome available.
Prefer deleting the test over letting it rot. It probes with `load` rather than `save` or
`delete` so it never writes a real profile or exercises the destructive path against a live
project.

✅ **PHASE 1 IS COMPLETE AS OF 2026-08-30.** The one thing that was never part of it, the
impersonation gap in § 88.1c, is built and is § 90.1. ⚠️ Read § 90.1 rather than § 88.1c for
how it works: the fix § 88.1c prescribed is not sufficient on its own and that entry now says so.

⚠️ **AND THE UNPLUGGED LAN RUN IS NOT OUTSTANDING.** 🧑 confirmed on 2026-08-31 that it has been
done. Several documents said otherwise and they are corrected in the same commit as this line.
**The account layer is the thing most likely to regress it**, because a boot that waits on UGS is
exactly what an unplugged venue produces, so re-run it after any change to `PlayerAccount`'s boot
path. § 88.1d is the bound that protects it.

---

## 89 · The profile, the stats and the match history ⚠️ IN PROGRESS 2026-08-30

Phase 2 of `FUTURE.md`, commissioned off its § 19.2 prompt. Phase 1 (§ 88) is under it, and the
`player-account` endpoint was re-proved before any of this was planned, per that prompt's own
VERIFY FIRST block: `ugs cloud-code scripts list` reports `player-account`, and the `Ugs` PlayMode
category came back **4/4 with `total="4"`**, which is the check `FUTURE.md` § 0.5 rule 9 asks for
rather than a green tick over an empty run.

**What a match now produces.** The host counts one `MatchRecord` covering all four seats, mints it
a `MatchId` at the whistle, broadcasts it to every peer, and each peer submits its own line to the
`match-record` Cloud Code endpoint from its own authenticated session. The endpoint accumulates
that player's `PlayerProfile` and stores the record in a 100-entry history. Nothing in the loop is
per event: a Hero Strike match carrying nine hundred passive-defence ticks costs one call per
player.

**Where the code went**, per `FUTURE.md` § 0.5 rule 3:

| Piece | Where |
|---|---|
| `MatchRecord`, `PlayerMatchStats`, `MatchRecordRules` | `Packages/com.tumbangpreso.core/Runtime/MatchRecord.cs` |
| `PlayerProfile`, `CareerTotals`, `ProfileRules` | `Packages/com.tumbangpreso.core/Runtime/PlayerProfile.cs` |
| Host-side counting | `Assets/TumbangPreso/Runtime/MatchStatsCollector.cs` |
| Local cache, offline queue, submission | `Assets/TumbangPreso/Runtime/Net/CareerStore.cs` |
| The one Cloud Code request | `Assets/TumbangPreso/Runtime/Net/CloudCode.cs` |
| The server, and the authority | `ugs/cloud-code/match-record.js` |
| The career page | `Assets/TumbangPreso/Runtime/UI/ProfileOverlay.cs` |
| The end-of-match summary | `MatchResult.OnRecordReady` |

⚠️ **No rate is ever stored.** Every rate on the profile is two counts divided at read time by
`MatchRecordRules.Rate`. A stored rate cannot be re-derived after a balance change and cannot be
summed across two seasons, and adding two seasons together is the first thing anybody does with a
career page.

⚠️ **Nothing in this phase awards XP, a level or a rank.** The fields are on the document from day
one so no profile written now has to be migrated later, and Phase 4 and Phase 9 fill them.
`PlayingAMatchAwardsNoXpAndNoRank` asserts it. The header draws neither: an empty rank badge on
every account in the game teaches every player that the game has a rank.

### 89.1 · "Retrievals under pressure" needed a threshold, and it is derived rather than picked

`FUTURE.md` § 2.2 asks for retrievals and retrievals under pressure, and names only the reason
(`VISION.md` § 0: the tension is the retrieval). It does not say how close is close.

`MatchRecordRules.PressureRadius` is **2.30 m** and every part of it is measured.
`Balance.LungeSpeed²/(2·Balance.Friction)` is the distance the taya's dash covers, which is
7.746²/(2·30) = **1.00 m**, because `CLAUDE.md` § 4 requires every impulse to be written as a
distance and solved for a speed; `Balance.LungeTagRadius` is the **1.30 m** the sweep then reaches.
So the stat asks the only question worth asking of a pickup: could the defender have taken you for
it, right then, without moving first.

⚠️ **It lives in `MatchRecordRules`, not in `Balance`.** `Balance` holds numbers the MATCH reads,
and nothing in the match reads this one. A stat threshold sitting among them is the next reader's
excuse to make gameplay depend on it. `ThePressureRadiusIsTheTayasStandingLungeReach` fails if
anybody replaces the arithmetic with a literal.

⚠️ **Only your own tsinelas counts as a retrieval**, which is the line `Carrier.NotifyHolding`
already draws for the hero economy. Picking up somebody else's is a denial play and a good one,
but it is not the run the game is built around and it carries none of the same risk.

⚠️ **The per-tsinelas record is keyed on `Slipper.SeatOfOrigin`, never on `OwnerSlot`**, which is
§ 78.1's fault one object further on. `OwnerSlot` is state the game rewrites every round and
`SliceRunner.EquipOwnedSlippers` sets the taya's to -1, so a record read off it would have no
slipper for whichever seat was defending, and a per-tsinelas win rate would under-count the
taya's shoe forever. The scene is searched once per match rather than once per seat.
⚠️ `MatchBootstrap`, the headless probe path, never assigns `SeatOfOrigin`, so a probe match
records an empty slipper id. That is correct rather than a gap: a probe has no player whose
record it could belong to, and `ProfileRules.Apply` skips an empty id instead of inventing one.

### 89.2 · "Longest survival as last attacker" is an interpretation, because nothing is eliminated

`FUTURE.md` § 2.2 asks for *"longest survival as last attacker"*. That is a stat from a game where
players go out. **This game eliminates nobody.** A tag costs a teleport, a stagger and the whole
trip again, and the round carries on with all four.

The reading that survives contact with the rules is **the last of the three attackers not yet
caught this round**: while you are the only one the taya has not taken, you are the only one they
can still take. `MatchStatsCollector` tracks it off `RoundDirector.Tagged`, opens the clock when
exactly one attacker is untagged, and closes it when that one is tagged or the round ends.

**This is written down because it is a decision rather than a derivation**, and the next person to
read § 2.2 will reach for elimination and find none. If it is the wrong reading, the fix is a
different rule here, not a new event in the match.

### 89.3 · ⚠️⚠️ EACH PEER SUBMITS ITS OWN LINE, WHICH DEPARTS FROM THE LETTER OF § 19.2

`FUTURE.md` § 2.3 says *"the host writes the record and that is a known hole, not an oversight"*,
and § 19.2's "Done when" says the whole match costs **one endpoint call**. The obvious build is the
host calling the endpoint once and the endpoint writing all four career documents. **That is not
what shipped**, per § 0.5 rule 11, and this is the argument.

The hole § 2.3 names is that **the host authors the numbers**. It still does: every count in the
record comes from `MatchStatsCollector` on one machine, and a modified host can lie about all of
them. Phase 8 is what closes that, and nothing here claims to.

Letting the host also **write** three other people's career documents is a second hole and a much
worse one. It is the difference between spoofing a match you played in and editing a stranger's
account: a host who never plays another game with you could still rewrite your career forever,
because the endpoint would have to accept any player id its caller named.

So the record is broadcast to every peer, and **each peer submits only its own line, from its own
authenticated session**. `match-record.js` refuses a record that `context.playerId` is not in, and
writes only that player's documents. It costs **one call per player per match** instead of one per
match, which still satisfies what § 0.3 and § 19.2 step 3 are actually protecting: never one call
per event.

⚠️ **A second thing falls out of it, and is the reason not to reverse this casually.** An offline
or LAN-only peer has no UGS id, only the local token `GameSettings.MintToken` mints. Under the
host-writes-everybody design the server would create a career document keyed by that token which
nothing will ever read. Under this one it simply does not call: it keeps a local career and queues.

### 89.4 · What § 2.1 asks for that is not on the screen, and which phase owns each

The career page draws the header card, the career strip, the mode tabs, the stat blocks, the paged
match history and the match detail. Four things in § 2.1 are deliberately absent, and each would be
an empty box today:

| § 2.1 item | Why not now |
|---|---|
| Avatar | `FUTURE.md` § 1.4 is still an open argument (an in-game avatar builder rather than a photo upload) and 🧑 has not answered it. There is nothing to draw. |
| Rank badge, peak rank | Phase 9. The fields are on the document; drawing an empty badge teaches every player the game has a rank. |
| Achievement and highlight shelf | Phase 10 owns achievements. |
| Compare with a friend | Phase 6 owns friends. There is nobody to compare against. |

⚠️ **Level and border are a different case and are not in that table**, because the FIELD shipped
and only the awarding did not. Phase 4 fills it with no migration.

### 89.5 · The protocol is 15, and § 88.4's probe duplication is resolved

⚠️ **`NetSession.ProtocolVersion` is 15**, for the one new message `MatchRecord`
(`MatchRpc.BroadcastMatchRecord`), which carries a whole finished match to every peer. A peer
without the handler plays the match correctly and then silently gets no end-of-match summary and no
career entry for a game it played, which is exactly the quiet kind of wrong this number exists to
turn into a refusal. **Both machines rebuild off this branch.**
⚠️⚠️ **AND THE MESSAGE IS BIGGER THAN A PACKET, WHICH THE DEFAULT DELIVERY WOULD NOT HAVE TOLD
ANYBODY.** Every other named message in `MatchRpc` is tens of bytes and takes the default
`ReliableSequenced`. A `MatchRecord` is four players times twenty-six fields of JSON and
**measures 2312 bytes** at full length;
`ReliableSequenced` cannot split a message, so an oversized one is refused by the transport,
the host logs a line nobody reads, and every client silently gets no summary and no career
entry, which is precisely the failure the version bump above exists to make impossible. It goes
`ReliableFragmentedSequenced`. ⚠️ Do not size this against a 1500-byte MTU:
`MatchRpc.PoseDelivery`'s note records that they play over Hamachi, a VPN with a smaller MTU and
real loss, and that the relay path *"was not better designed, it was luckier"*.
`AFullMatchRecordNeedsMoreThanOnePacketAndIsSentFragmented` measures the record and fails if
somebody puts the delivery back.

⚠️ `ChatAndLobbyChromeTests.TheProtocolCarriesEveryRosterBump` caught the bump on the first
EditMode run of this work, which is the whole reason that tripwire exists; it is re-armed at 15.

⚠️ **§ 88.4 said the probe's hand-written copy of `PlayerAccount.CallCloudAsync` had to stay a
copy, and that is no longer true.** It kept the duplicate because the method was private and
widening it *"would put a seam in shipping code for one probe"*, while naming the cost outright:
*"if the call shape drifts, the probe passes while the game fails, which is the worst outcome
available."* Phase 2 needed a THIRD copy for the career endpoint, which is the point at which two
copies with a note becomes the failure the note was warning about. The request now lives in
`Net.CloudCode`; `PlayerAccount` calls it and so does the probe. A shared helper the game uses is
not a seam, and the drift § 88.4 feared cannot happen. `TheCareerEndpointAnswersALoad` is the new
`Ugs` test, and it is the only thing that catches a script that was written but never deployed,
because `CareerStore` is built to keep a local career quietly when the service is unreachable.

### 89.6 · `ProfileRules` is written twice, in C# and in JavaScript

`ugs/cloud-code/match-record.js` is `ProfileRules.cs` and `MatchRecordRules.cs` again. Cloud Code
cannot import the C#, and the C# cannot run in Cloud Code, so this is the same trade
`player-account.js` records about `DisplayNameMax`, applied to a whole file instead of a constant.

- **The C# is the specification** and carries the tests. When the two disagree, the JS is the bug.
- **The symptom a player sees is a career that changes the moment they come back online**, because
  `CareerStore` is an optimistic local cache and the server's profile REPLACES it whole on the next
  answer. There is no merge and there must not be: two counters that both claim to know how many
  matches you have played cannot be reconciled without the records that produced them, and the
  records are on the server.
- **If a rule changes in `ProfileRules`, it changes in the JS in the same commit.** Every function
  in the script names the C# member it mirrors, so the two can be diffed by eye.

### 89.7 · The offline path, and the LAN run it must not regress

A match played with the internet unplugged updates the local profile, lands in the local history,
and joins a queue capped at 20 matches. `CareerStore.FlushAsync` sends it on the next successful
sign-in, oldest first, and **stops at the first failure** rather than firing nineteen more requests
at a service that is not there.

⚠️ **The queue is written BEFORE the call is tried, not after it fails.** A process killed
mid-request is the case a queue exists for, and one only written in the failure branch has already
lost the record by then. A duplicate submission is free: `ProfileRules.Apply` refuses a match id it
has already counted, on the server as well as on the client.

⚠️⚠️ **§ 88's warning applies to this phase too, and it is the thing to re-test.** The unplugged
LAN run is DONE and is a regression to protect, not an open task. Nothing in this phase sits on the
boot path, but `CareerStore` now writes `Application.persistentDataPath/career.json` and subscribes
to `PlayerAccount.Changed`, so **re-run the unplugged four-player match after this lands**, per
`FUTURE.md` § 0.5 rule 7.

⚠️ **A career cached for a different account is discarded rather than merged.** Two people share
this machine at a tournament; merging would hand somebody else's knockdowns to whoever signs in
next, which is worse than losing an offline queue. `OwnerId` is empty on a fresh install and adopts
the first account to write, so somebody who has never signed in keeps everything they played
offline.

---

### 89.8 · Verified

- **Core 164/164** (`dotnet test`), from 135 before this phase. 29 of the new tests are the
  record and profile rules: placements, clutch, idempotency, streaks, comeback denominators,
  history trimming, the sample-size gate and the pressure radius.
- **EditMode 236/236 -> 241/241**, read from `Logs/tests.xml` rather than from the exit code.
  ⚠️ The first run was 235/236: `TheProtocolCarriesEveryRosterBump` caught the `MatchRecord`
  message, which is the tripwire working. Re-armed at 15.
- **All five editor checks OK in one launch** (`Checks.RunAll`), and a clean Windows player
  on the Desktop. `MapGeometryCheck` prints per-prop FAIL lines for Eskinita dressing that
  it does not gate on; the run's own verdict is OK and nothing in this phase touches a scene.
- **`CareerAndCloudCodeTests` is new and reads source as text**, because both faults it looks
  for are invisible to every other test here: a second hand-written Cloud Code request still
  works until it drifts, and nothing on this machine compiles the JavaScript. It gates the
  one-helper rule and pins `PLAYER_COUNT`, `HISTORY_LIMIT`, `APPLIED_ID_MEMORY`,
  `DISPLAY_NAME_MAX` and `DISPLAY_NAME_MIN` in the two scripts to the core constants. **That
  makes § 88.1a a gate rather than a paragraph** asking somebody to remember.
- **`Ugs` category 5/5.** `player-account` answered `{"profile":""}` and `match-record`
  answered `{"applied":false,"profile":""}`, which is the correct answer for a player who has
  never finished a match.
  ⚠️⚠️ **AND THAT PAIR OF ANSWERS TURNED OUT TO PROVE LESS THAN IT READS AS PROVING. § 90.5.**
  Both probe with `load`, which is the branch an ABSENT action falls through to, and Cloud Code
  was stripping the action because the scripts declared no parameters. `submit` had therefore
  never run and no career had ever reached the server, with every probe green throughout. Fixed
  and redeployed 2026-08-30; a parser now fails if a script reads a parameter it does not declare. Both went through `Net.CloudCode`, so the probe now proves the
  transport the game uses.
- **`BotBehaviourProbe` 3/3**, whole matches in both modes on both maps, which is what exercises
  the throw, retrieval, shove and lunge hooks in `Carrier` and `CombatVerbs`.

⚠️ **STILL TO DO ON THIS PHASE, AND IT IS NOT CODE:** the unplugged four-player LAN run, per
§ 89.7. It is a regression to protect, not an open task, and this is the phase most able to
break it. **§ 90.4 is what is automated in its place, and what to check when the four machines
are on the table.** It applies to § 90 as well as to this phase: the protocol is 16 now, so every
machine has to be rebuilt before the run means anything.

---

### 89.9 · Two things found by reviewing this phase rather than by running it

Both were caught before the branch was played, and both are the kind that pass every test and
then fail in front of people.

**The record broadcast would have been silently refused by the transport.** Covered in § 89.5:
it is 2312 bytes, `ReliableSequenced` cannot split a message, and the whole feature would have
worked perfectly in single player and done nothing at all online. The measurement is now a test.

**The mastery list overlapped the stat block on the career page.** Laid out at 660 by 120 in the
bottom-left of the panel, its top edge sat at 188 px from the floor while the last stat row's
box reached down to 156, and its own bottom ran into the REFRESH and CLOSE row. Eighteen
characters plus six heroes is a grid rather than a footnote, so it moved behind a CHARACTERS
button into its own panel, the same shape the match detail already uses. ⚠️ It is also only
rebuilt while that panel is open: a string for eighteen characters rebuilt on every `Changed`
from a screen nobody is looking at is the shape `Hud`'s per-frame rebuild took an eighth of the
probe's frames with.

⚠️ **And the stat block had 14 slots for a 16-row list**, so the two rows appended after the
fact (average time to first throw, distance per round) fell off the end silently, which is
exactly the pair nobody would notice missing. They are in the list proper now and a
`Debug.Assert` fails if the block is ever smaller than what it is asked to write.

---

## 126 · The full PlayMode suite had never been run on this commit, and it was 42 red ⚠️⚠️ 2026-09-03, branch `ui-redesign`

The handoff into this session said it in its own words: *"A full PlayMode suite was never run on
this commit, do that first."* It was run first. **155 tests, 113 passed, 42 failed, in 22
suites**, against a commit whose targeted runs were all green (core 431/431, EditMode 292/292,
`Checks.RunAll` 6/6, `InputSurfaceProbe` 5/5).

⚠️⚠️ **FORTY OF THE FORTY-TWO WERE ONE PROBE'S LEAKED STATIC, AND THE SORTING RULE THAT PROVED IT
TOOK ONE COMMAND.** Every failing suite except two sorts at or after `InputSurfaceProbe`
alphabetically, and the two that do not (`AspectRatioProbes`, `CarryTests`) are genuine and
pre-existing. That is not a coincidence anybody had to be clever about: it is what a leaked global
looks like in a run that discovers its tests in name order.

### 126.1 One probe threw, left the thumb layer switched on, and twenty suites reported other people's bugs

Three faults in a chain, and every one of them is a rule this repository already had.

1. ⚠️ **`InputSurfaceProbe.Measure` held a `Camera` across a scene change.** `Camera.main` is read
   once at the top of the method and the settle frames below it let a screen's own `Start` run; a
   screen that routes onward calls `SceneFlow.Go`, the scene unloads, and the next
   `camera.targetTexture = next` throws `MissingReferenceException`. **Guarded per shape now**, and
   the report says which scene navigated away rather than the probe dying on it.
2. ⚠️⚠️ **THE RESTORE OF `TouchHud.ForceVisible` SAT AFTER THE SWEEP RATHER THAN IN A `finally`.**
   So the throw above left the thumb layer **on for every test that ran afterwards**, on a machine
   that has no touchscreen and where no other test expects one. It is in a `try`/`finally` now.
   `yield return` inside a `try` with a `finally` is legal in a C# iterator, and the `finally` also
   runs when NUnit disposes an abandoned enumerator, which is the case that matters.
3. ⚠️⚠️ **AND WITH THE LAYER ON, `ScreenFocus`'S TOUCH PAD THREW ON EVERY SCREEN.** `TouchHitArea`
   is a `MaskableGraphic`, `Graphic` requires a `CanvasRenderer`, and `AddComponent` did not apply
   that requirement to the subclass, so the pad object came up with a graphic and no renderer and
   the base class threw `MissingComponentException` the moment it tried to draw.

⚠️⚠️ **AND FAULT 3 IS A SHIPPING BUG ON ANDROID, WHICH IS THE REASON THIS ENTRY IS FIRST.** On a
phone `TouchHud.ShouldShow` is **true**, so a pad is built for every control on every screen: the
front end this exception breaks is the one every phone player opens, on the .apk this batch was
about to build. It was invisible on the desktop for exactly the reason that makes it dangerous, a
Windows machine with no touchscreen never builds a pad at all, and it surfaced only because a probe
forces the layer on and then only because that probe leaked it.

**Fixed by construction rather than by remembering:** `[RequireComponent(typeof(CanvasRenderer))]`
on `TouchHitArea`, `typeof(CanvasRenderer)` in the pad's own constructor call, and a third check on
the reuse path for a pad some earlier layout built without one.

⚠️ **The two genuine reds, kept separate on purpose:**

- **`CarryTests`, 0.092 m against a 0.05 m bound.** This is § 93 and it is now on its third
  recorded sample (0.084, 0.092, 0.092). `CLAUDE.md` § 7.1 and § 94.8 both say not to widen the
  bound and nothing in this batch touches `Carrier`, the animator or `LateUpdate`. **Still open.**
- **`AspectRatioProbes.TheCharacterScreenSurvivesEveryAspectRatio`:** a `Label` authored at 14
  units against the 18-unit readable floor.

### 126.2 The thumb floor was unreachable by construction, because padding cannot make room

§ 125.13 left this as *"the converted screens get a taller row on touch, and that test goes
green"*, with the shortfall at **1519 measurements across twelve shapes** and the cause named as a
layout pass. Reading the report rather than the prose changed what the pass had to be.

⚠️⚠️ **EVERY ONE OF THE 1519 REPORTED A SIZE EQUAL TO THE CONTROL'S OWN ARTWORK, WHICH MEANS THE
PAD HAD GROWN BY ZERO UNITS, NOT BY TOO FEW.** `ScreenFocus.ApplyTouchTargets` grows a hit area
only as far as the nearest neighbour allows, and it takes **half** the gap because the neighbour is
growing too. The settings rows are stacked with **no gap at all** (a rebind row's height IS its
keycap's height, in a list with `spacing = 0`), so the clamp came out at zero every time. **The
number was not "not enough padding". It was "there is nowhere to pad into".**

The shape of it, from `Logs/input-surface.txt`, and it is almost entirely one screen and one axis:

| Count | Screen and control | Size | What is wrong |
|---|---|---|---|
| 864 | `SettingsPanel` rebind keycaps | 428x46 | height |
| 96 | `SettingsPanel` sliders | 344x34 | height |
| 48 | `SettingsPanel` checkboxes | 688x34 | height |
| 48 | `SettingsPanel` BACK / RESET ALL | 228x60 | height |
| ~90 | `ModeSelect`, `MultiplayerSetup` primaries | 6xx x101 | height |
| 24 | `MatchSetup` lobby rail | 40 tall | height |
| 20 | `SettingsPanel` scrollbar | 14 wide | width |

**Widths are almost all fine. This is a row-height problem**, which is why it can be answered once
rather than in 79 places.

⚠️⚠️ **SO THE PASS IS `ScreenFocus.MakeRoomForThumbs`, AND IT RUNS BEFORE THE PADDING RATHER THAN
INSTEAD OF IT.** `Rebuild` now makes room, forces a layout rebuild, and then pads. `CLAUDE.md`
§ 4a's answer to every one of these is construction rather than a checklist, and `ScreenFocus` is
installed by `MenuKit.BuildCanvas` and `ConvertedScreen.Start`, which between them are **every
screen in the game**. A screen added next month gets this without anybody remembering.

- ⚠️ **IT GROWS THE BOX A LAYOUT GROUP OWNS, WHICH IS RARELY THE CONTROL.** On the converted
  settings panel the slider's parent is `MasterVolumeRow` and the vertical group is `Content` above
  that, so writing `minHeight` on the slider would have reached nothing at all. `LayoutRowFor`
  walks up to the child OF the group, which is what makes one rule cover the code-built rows, the
  converted rows, the lobby rails and the tab bar together.
- ⚠️ **AND IT ASKS `childControlHeight`, NOT "IS THERE A GROUP".** A group that does not control
  its children's height ignores `minHeight` entirely, so writing one would be a silent no-op and
  this pass would report itself as done having changed nothing.
- ⚠️⚠️ **THE LAYOUT IS FORCED TO REBUILD BETWEEN THE TWO PASSES.** `LayoutElement.minHeight` only
  marks the group dirty; the rects do not move until Unity's next layout pass, and
  `ApplyTouchTargets` reads rects. Without the forced rebuild the padding pass would compute its
  clamps against the rows it had just replaced. It is forced only when something actually moved.
- ⚠️ **168 UNITS, AND TWO BOUNDS AGREE ON IT.** `TouchMetrics.MinTargetUnits` (144) plus
  `MinGapUnits` (24) is what lets two stacked targets both reach the floor with a gap between them;
  144 plus the deepest inset in `UiRows` (22, the stepper arrows) is 166. 168 satisfies both.
  `UiRows.TouchRowHeight` and `ScreenFocus.TouchRowUnits` are the same sum on purpose.
- ⚠️ **THE DESKTOP IS UNCHANGED, BYTE FOR BYTE.** All of it is behind `TouchHud.ShouldShow`, which
  is false on a machine with no touchscreen, so every layout probe photographing this front end at
  the nine desktop shapes measures exactly what it measured before.

⚠️⚠️ **AND THE SCROLLBAR IS EXEMPT RATHER THAN PADDED, WHICH IS A DISTINCTION AND NOT AN EXCUSE.**
`MinTargetUnits`'s own words are *"the smallest a touch target may be"*, and that number is about
how accurately a thumb can PRESS a discrete control. A scrollbar is dragged, it already says where
it is, and on a phone the thing a thumb drags is the list. Held to 144 it would spend a fifth of
the settings panel on a readout. It is **44 units wide on touch** instead (the width
`UiRows.ArrowWidth` already uses, about 3 mm on a 1080-tall phone, against 14 units which is about
one), and `InputSurfaceProbe` skips scrollbars in the floor check and prints them in the report
instead so the exemption is visible rather than silent.

### 126.3 The move stick drew as a square because `Mathf.SmoothStep` is not `smoothstep`

§ 125.13's entry read *"the code now assigns a generated circular `TouchSkin.Ring` sprite instead,
and the last render still shows the square, so the change is either not reaching the Image or the
generated sprite's alpha is being ignored"*, and it pointed the next reader at the sprite:
*"suspect the sprite, not the layout: a null `Image.sprite` draws as a white rectangle"*.

**It was neither. The sprite was there and its alpha channel was flat**, and what settled it was
measuring the render instead of re-reading the code. Sampling one row through the stick's centre on
`Logs/shots-touch/touch-Classic-20-9-phone-v3.png`:

```
x 140..234   (253,174,123)   base only, composited alpha ~0.111
x 235..424   (252,189,146)   base + knob, the knob adding ~0.27
```

140 is `StickCentreX - 190` and 235 is `StickCentreX - 95`, which are exactly the base and knob
half-widths, and **the fill is uniform with no hole in it**. A null sprite draws at full alpha; a
ring has a hole. Neither matches. A uniform partly-transparent square does.

⚠️⚠️ **THE CAUSE IS ONE LINE OF SEMANTICS. `Mathf.SmoothStep(a, b, t)` RETURNS A VALUE BETWEEN
`a` AND `b`; GLSL's `smoothstep(edge0, edge1, x)` RETURNS ONE BETWEEN 0 AND 1.** `BuildCircle` was
written in the GLSL reading:

```csharp
float alpha = 1.0f - Mathf.SmoothStep(outer - feather, outer, r);   // ~0.50 for every pixel
if (!filled) alpha *= Mathf.SmoothStep(inner - feather, inner, r);  // ~0.42 for every pixel
```

With `outer` 0.5 and `feather` 1/128 the first term is always about 0.4961, so the disc came out at
a uniform alpha 0.50 and the ring at 0.21. Measured against the render: 0.50 x 0.85 (the knob
colour's own alpha) x the layer opacity is the 0.27 the knob added, and 0.21 x 0.85 is the 0.111
the base composited at. **The arithmetic reproduces the photograph.**

`TouchSkin.Edge` is a real edge ramp now and carries this note. ⚠️ **The other four
`Mathf.SmoothStep` calls in the project are correct** and were checked before the fix was written:
`SkyEvent`, `VolcanicCooling` and `GhostPetCompanion` all pass an already-normalised 0..1 as `t`,
which is the signature Unity actually has.

### 126.4 The six stored quality levels are asserted now, and the note that protected them is stale

§ 125.14 asked for *"a cheap EditMode test asserting the six levels against `AntiAliasModes`"*.
`AntiAliasModes.QualityLevelSamples` is that table as DATA rather than as prose in a header, and
`QualitySettingsAssetTests` reads the six stored values straight out of
`ProjectSettings/QualitySettings.asset` with a `SerializedObject`.

⚠️ **IT READS THE ASSET RATHER THAN WALKING `QualitySettings.SetQualityLevel`.** Selecting each
level to read it is a WRITE, on the exact field the test exists to protect. The asset read needs no
play session and no editor camera and the three cases run in milliseconds, which is the bound
§ 124.11 says belongs in the forty-millisecond test rather than in a twelve-minute one.

⚠️⚠️ **AND READING THE HEADER PROPERLY FOUND THAT ITS ARGUMENT NO LONGER HOLDS.** It says Ultra is
4 *"so that it matches `Default` ... so matching the two means the ordinary case touches nothing"*,
and that was true when `Default` was index 3, MSAA 4x + FXAA. **`Default` is 1 now, FXAA alone,
whose `Samples` is 0**, changed for the measured tonemap reason further up that file. So the two no
longer match and the protection that sentence describes is not the one in force. The table is kept
at the RENDERING intent rather than bent to suit the default, because `Apply` overwrites the active
level at boot from the player's own setting: **the stored number is never what the game renders
with.**

⚠️ **MEASURED RATHER THAN ASSUMED: a full batchmode PlayMode suite, 155 tests and eighteen minutes
of play, left the asset completely clean.** The write-through the header warns about is an
INTERACTIVE editor behaviour. It is real, and it is not something a batch run reproduces, so this
test is not fragile under a headless gate. ⚠️ **If it ever goes red where nobody edited the asset,
the level that moved names the editor's build target**: `m_PerPlatformDefaultQuality` puts
Standalone on 5 (Ultra) and **Android on 2 (Medium)**, so building the .apk moves which row is at
risk.

### 126.5 Nothing scrolled a focused control into view, and the scrollbar was carrying it

⚠️⚠️ **A PAD WALKING THE SETTINGS LIST SELECTED ROWS NOBODY COULD SEE.** Unity's input module moves
the selection and does nothing about scrolling, and neither `ScreenFocus` nor `UiInputModule` had a
line about it. The settings panel is about forty rows in a viewport showing around ten, so pressing
DOWN eleven times left the highlight below the fold with the list still at the top.

**That is `CLAUDE.md` § 4a's § 96 in a new costume.** `InputSurfaceProbe` check 1 walks
`selectOnDown` and asserts every control is on the path, and every control was: the probe proved
the plate was there, not that anybody could get to it. `InputSurfaceProbe.InsideOwnViewport` exists
to skip exactly these rows, and its note (*"a control scrolled out of its own viewport is not
blocked, it is below the fold"*) is the other half of this bug written down a fortnight early.

`ScreenFocus.FollowSelectionIntoView` scrolls the owning `ScrollRect` when the selection moves.
⚠️ **Once per selection change, never per frame**, because writing `normalizedPosition` every frame
fights the player's own drag and the wheel, and `SettingsWheelProbe` is the test that would find
that the hard way. ⚠️ **And only when the row is actually out of view**: snapping every selection to
the middle makes the list lurch on every press, which reads as the screen fighting the player.

### 126.6 The rebind list is two pages now, and the panel used to show a pad player a list of keys

§ 125.13's first open item: *"`Rebinding` can now answer per device and rebind per device without
disturbing the other, so the data is all there; what is missing is a device toggle on the panel so
a pad player can SEE their own bindings. Done looks like: one control at the top of the rebind list
switching every row between keyboard and pad."* That is what shipped, and building it found three
things the data being "all there" did not cover.

- ⚠️⚠️ **KEYBOARD AND MOUSE IS ONE PAGE, AND THE EXISTING PER-DEVICE OVERLOAD COULD NOT SAY SO.**
  `DisplayNameFor(asset, action, "<Keyboard>")` matches one prefix, so it answers "-" for SPECIAL
  ABILITY and LUNGE, which are `<Mouse>/leftButton` and `<Mouse>/rightButton`: **two of the
  most-used controls in the game would have read as unbound on the page that exists to show
  them.** `Rebinding.PathIsFor` groups by `InputDeviceKind` instead, and the rebind operation gets
  two `WithControlsHavingToMatchPath` calls for the same reason.
- ⚠️⚠️ **THE CANDIDATES HAVE TO BE RESTRICTED TO THE PAGE'S DEVICE OR THE PAGE IS A LIE.**
  `TryRebind` writes the override onto the binding for the device that was PRESSED, so without a
  restriction a player on the GAMEPAD page could press a key, have their keyboard binding silently
  changed, and watch the row in front of them not move.
- ⚠️⚠️ **AND `TryRebind`'S FALLBACK WAS § 125.6 WAITING TO HAPPEN AGAIN.** When no binding matched
  the pressed device it fell back to `indices[0]`, which is the KEY. `ScreenInputCatalogue` records
  a `null` pad path as a written-down answer (`ToggleFullscreen`: a phone has no window), so a pad
  press aimed at such a row would have written `<Gamepad>/...` straight over the keyboard binding,
  which is the exact fault that method already carries a two-paragraph warning about. **It refuses
  now**, and `Rebinding.HasBindingFor` is the polite half that stops the player getting that far.

⚠️ **The page opens on the device the player is holding**, off `LastInputDevice`, which is that
class's own argument reused rather than a second one: *"a player who picks up a pad mid-match has
told you which glyph they want by picking it up."* ⚠️ **Touch is not a third page**: the thumb layer
is not rebound by path at all, it has its own screen reached from a row further down this same
list, and a third tab where one of the three leads somewhere else entirely is what `CLAUDE.md` § 6.2
calls overwhelming.

⚠️ **The live page is relief, not colour**, which is what every other tab strip in this front end
does and what `GodotTheme` and `CustomCharacterScreen` both already argue: a live tab is a
statement about where you are, not a second "press me". It also survives a colourblind player,
which is `FUTURE.md` § 16.1.

⚠️⚠️ **AND THE PAD CAN BACK OUT OF A REBIND WITHOUT A KEYBOARD, WHICH NEEDED A CHANGE TO A GUARD
WHOSE COMMENT EXPLAINED WHY IT COULD NOT BE CHANGED.** The operation cancels through `<Gamepad>/
buttonEast` on the pad page, so `Update`'s *"ESC is the rebind's cancel while one is listening"*
guard was no longer true there and Escape would have done nothing at all during a pad rebind.
`CLAUDE.md` § 6.3 twice: *"Escape backs out on every screen, always, innermost layer first"*, and
*"a player who learns Escape is reliable and then meets one screen where it is not has learned that
it is unreliable."* The panel cancels the rebind itself on that page and consumes the key.

### 126.7 Rumble, on the four moments that change the player's own situation

`FUTURE.md` § 14 asked for *"rumble on knockdown, tag and can reset"* and § 125.13 recorded it as
the one row of Phase 14 that did not ship. `InputLayer.Rumble` is four cues rather than one volume.

⚠️⚠️ **THE FOURTH CUE IS BEING TAGGED, IT IS THE STRONGEST OF THE FOUR, AND IT IS NOT ON THAT LIST
OF THREE.** Being tagged pays the victim nothing, so `Hud.OnScored` says nothing to them at all,
and that file already records what that leaves: the `TAGGED!` toast is *"the only thing on their
screen that explains why they are suddenly somewhere else and cannot move"*, and in first person it
spawns inside their own head. **The moment a player most needs telling is the moment the score
system has nothing to say to them**, which is the gap a haptic is actually for.

- ⚠️ **THE TWO MOTORS ARE DIFFERENT INSTRUMENTS**, so a thump is mostly low and a snap is mostly
  high. One number at four volumes would have made every event the same event, which is
  `docs/VISION.md` § 2 rule 3 about effects applied to a hand.
- ⚠️ **OVERLAPPING PULSES TAKE THE MAXIMUM, NEVER THE SUM**: `CLAUDE.md` § 4's stun rule applied to
  a motor. Two events in the same tenth of a second are ordinary (a tag and a sabotage, a knockdown
  and a reset) and adding them clips both motors to 1.0 and turns two distinct cues into one buzz.
- ⚠️⚠️ **IT FIRES ON THE LOCAL PLAYER'S OWN EVENTS**, which is `Hud.OnScored`'s existing rule for
  the toast rather than a new one. The exception is the can going back up, which changes what
  everybody may do next and is the softest of the four.
- ⚠️⚠️ **THE CAN-RESET CUE IS IN `MatchInstaller`'S LAMBDA AND NOT IN THE HUD**, because that
  lambda is the one owner of `UprightChanged` and `Hud.TrySubscribeRound` carries the receipt for
  what a second subscriber costs: *"one event, two subscriptions, two identical calls, and a toast
  timer restarted mid-fade."*
- ⚠️⚠️ **A MOTOR LEFT RUNNING DOES NOT STOP WHEN THE GAME DOES.** A pad holds whatever speed it was
  last given until something tells it otherwise, so a quit or a lost focus mid-pulse hands the
  player a controller buzzing on their desk. `Rumble.Stop` is called from `OnDisable`,
  `OnApplicationQuit` and a lost focus. **This is the one piece of state in the project that
  outlives the process.**
- ⚠️ **THE COUNTDOWN IS UNSCALED TIME.** 0.24 s is a number about a person's hand, not about the
  simulation, and the probes drive `Time.timeScale`.
- ⚠️ **IT IS SILENT WITH NO PAD, BEFORE THE DRIVER OBJECT IS EVEN CREATED**, which is what makes it
  free in a batch test run: no `Gamepad.current`, no driver, nothing to leak between scenes.
- ⚠️ **AND IT HAS AN OFF SWITCH**, in the CONTROLS list beside the bindings rather than in the
  display group, because that is where somebody looking for a controller setting will look.
  `FUTURE.md` § 16.2 is an accessibility list and a haptic nobody can turn off is on it.
  `GameSettings.Rumble` is a `bool` initialised `true`, which is the safe shape for an upgrade for
  the reason `RenderStyle`'s note gives about `JsonUtility` and field initialisers.

### 126.8 ⚠️⚠️ OPEN: THE FULL PLAYMODE SUITE IS NOT A RELIABLE GATE ON THIS BRANCH, AND TARGETED RUNS ARE WHY NOBODY KNEW

**This is the largest thing this session found and it is not fixed.** It gets its own entry because
every PlayMode number in every handoff in this file is quoted from a targeted run.

**Two full runs, same machine, an hour apart:**

| | Run 1, HEAD `550ba0f`, no edits | Run 2, after the three fixes in § 126.1 |
|---|---|---|
| Result | 155 cases, 113 passed, **42 failed**, 1072 s | 155 cases, 114 passed, **41 failed**, 969 s |
| The `TouchPad` exception | 13 suites | **gone** |
| `InputSurfaceProbe` | failed (destroyed camera) | **passed** |
| `PlayerHubLayoutProbe`, `PhaseSurfaceLayoutProbe`, `QueueCardLayoutProbe`, `SettingsScrollProbe`, `SoloPracticeTests` | failed | **passed** |
| Red in run 2 and not in run 1 | | `MatchRecordIdentityProbe`, `ToneSweep`, `TrainingStreetProbe` x2, `UgsServicesProbe` x6, two more `MatchRunTests` |

⚠️⚠️ **THE COUNT BARELY MOVED AND THE RED SET LARGELY CHANGED, WHICH IS THE FINDING.** Eleven
suites went green and eleven different ones went red. **A gate whose red set moves is not measuring
the code.**

⚠️⚠️ **AND THE EXPERIMENT THAT SETTLES IT COST 105 SECONDS.** The nine suites carrying about twenty
of run 2's failures were re-run together on their own, with `-testFilter`, on exactly the code that
had just failed them:

```
31 cases, 29 passed, 2 failed, 105 s
```

**Twenty failures became two.** The two survivors are real and are § 126.9. Everything else was the
suite, not the code.

**What the reds actually say**, and none of the stack traces are in code this batch touched:

- `MissingReferenceException: the object of type X has been destroyed`, **inside the test**, at
  `SettingsWheelProbe.cs:117`, `SteeringTests.cs:177`, `UiClickProbe.cs:140`,
  `VolcanicZoneTests.cs:60`, and inside `ModelPreview.IsolateFromForeignLights`. Every one is a
  reference the test is holding across a `yield` that something else destroyed.
- *"the arena built no SliceRunner"*, *"No main camera in the arena"*, *"the guided route never
  installed"*, *"MatchSetup has no CharacterSelectPanel to open"*, *"the lobby must have a door to
  the account screen"*. Every one is a scene that did not come up the way the test expected.
- `UgsServicesProbe` x6: *"You are not signed in to the Authentication Service"* and *"The player
  is already signing in"*. That is a **live service and a shared session**, and six went red in run
  2 having passed in run 1 with nothing changed between them that touches authentication.

**So the class is cross-test lifetime leakage**: objects, statics, scenes and one cloud session
outliving the test that made them. § 126.1 is one instance, found and fixed. It is not the only one.

⚠️⚠️ **WHY IT WAS INVISIBLE.** § 94.8 records *"PlayMode, targeted: 15/15"* and then *"11/11 on a
second pass"*; § 125's verification is *"`InputSurfaceProbe` 5/5"*. **Those runs are honest and
they pass.** The suite only comes apart when it is run as one process, which is the one thing
nobody had done on this branch until this session was told to do it first.

**Done looks like** one of these two, and it is a decision rather than a task:

1. **Every PlayMode fixture tears its world down**, so no test can inherit one. That is the real
   fix and it is a pass over every file in `Assets/TumbangPreso/Tests/PlayMode/`. Start with the
   five named above, because their stack traces name the exact line that holds the stale reference.
2. **Or the suite is declared to run in named groups**, the groups go into `docs/TESTING.md` and
   `CLAUDE.md` § 7, and a single-process full run stops being quoted as a gate at all.

### 126.8b ⚠️⚠️ THE THIRD FULL RUN, 2026-09-03: 56 RED, AND THE EXECUTION ORDER NAMES THE CAUSE IN ONE LINE

**This entry has never had a full run on the commit it describes, and § 130.19 says so in as many
words. It has one now, on `16b8109`, and it is worse than either of the two above:**

```
155 cases, 99 passed, 56 failed, 780 s     (Logs/play-full.xml)
```

⚠️⚠️ **THE FIX IN § 130.10 WAS REAL AND REACHED FIVE FIXTURES OUT OF SIXTY.** `PlayModeWorld` was
written, and `SettingsWheelProbe`, `SoloPracticeTests`, `SteeringTests`, `UiClickProbe` and
`VolcanicZoneTests` were given the pair. **Fifty-five other fixtures were left with no reset of any
kind**, which is not a criticism of that entry (it fixed exactly the five it named) but it does
mean the property everybody then assumed the suite had, that no test can inherit a world, was
never true of nine tenths of it.

⚠️⚠️ **AND SORTING THE 155 CASES BY START TIME ANSWERS THE WHOLE QUESTION.** The run is **clean for
its first 57 cases**, with two known reds in it (§ 130.15's caption and `InputSurfaceProbe`'s own
touch-target case), and then from case 58 onward it fails almost continuously to the end. What sits
between case 57 and case 58:

| # | Time | Case |
|---|---|---|
| 50-54 | 06:23:57 | **`InputSurfaceProbe`, five cases** |
| 55-57 | 06:24:34 | `LandedHighlightTests` x2, `LataFloatProbe`, all green |
| **58** | **06:24:37** | `LoadoutSurfaceProbe`, *"MatchSetup has no CharacterSelectPanel to open"* |
| 59 to 154 | | **44 more failures out of 97 cases** |

**`CLAUDE.md` § 7 already names this fixture and says exactly what it does**: *"⚠️⚠️ RUN IT ALONE.
`InputSurfaceProbe` loads every scene in the build settings and opens every overlay it can
discover, so it is the most destructive fixture in the suite: in a twelve-suite run it took most of
the group down with it and the numbers were meaningless."* **It had no teardown**, so everything it
opened was still open for the remaining 97 cases.

⚠️⚠️ **AND THE OVERLAYS IT OPENS ARE THE HALF A SCENE UNLOAD CANNOT REACH, WHICH IS WHY § 130.10'S
FIX WOULD NOT HAVE BEEN ENOUGH EVEN IF EVERY FIXTURE HAD CARRIED IT.** Fifteen files in `Runtime/`
call `DontDestroyOnLoad` and **only six of them are services**. The other nine are SCREENS and JOBS
that need to survive one scene change: `MatchResult`, `PausePanel`, `MapPreviewSurface`,
`BootSting`, `MatchInstaller` and three diagnostics. `PlayModeWorld.Reset` deliberately left every
one of them alone, with the note *"those are SUPPOSED to survive"*, **and that sentence is true of
`GameServices` and false of a results board.** `MapPreviewSurface` is the worst of them: it loads
arenas **additively and caches them**, so a cached map landing inside another suite's test brings a
whole arena's lights, cameras and post stack with it. That is § 126.8's own *"No main camera in the
arena"* and `QueueCardLayoutProbe`'s destroyed `Camera`, from the two opposite ends.

**What was done, 2026-09-03:**

- ✅ **All 60 PlayMode fixtures carry a reset now**, not five. Forty-six got the `[UnitySetUp]` and
  `[UnityTearDown]` pair; fourteen that already own a `[UnityTearDown]` got the **setup half only**,
  which is deliberate and stated in each one: NUnit does not define an order between two teardowns
  of the same kind, three of those fourteen have an early `yield break` in theirs, and **the setup
  reset is the half that protects the fixture it is on**. With every fixture carrying it, nothing
  can inherit a world regardless of what any teardown did.
- ❌ **`PlayModeWorld.SweepPersistentLeftovers` WAS BUILT, MEASURED AND WITHDRAWN. § 126.8d.**
- ✅ **`Reset` no longer unloads the runner's own scene.** `MatchRunTests.SetUp` has carried the note
  since it was written (*"Unloading that takes the test framework's objects with it and the run dies
  rather than fails"*) and this method did not have the guard. The five fixtures that used it
  survived because the bootstrap scene happens to be empty by the time a test runs, **which is luck
  rather than a guarantee**.
- ✅ **And it never unloads the last loaded scene**, which Unity reports through `Debug.LogError`
  rather than by throwing, so the existing `catch (ArgumentException)` could not see it and the
  framework fails a test on any unexpected error log. It appeared once in each of the two full runs.
- ✅ **`UgsServicesProbe` reports SKIPPED in batch mode instead of eight false reds.**
  `NetIdentity.AttemptSignInAsync` refuses UGS sign-in outright when `Application.isBatchMode`,
  deliberately, because a headless run has no display and no Hub session token. So all eight cases
  were asserting that a guard the game ships does not exist, with the same message eight times.
  ⚠️ **This is not § 126.8's forbidden third category.** That ban is on a category meaning *"these
  tests do not work next to each other"*, which would hide a cross-test leak. This is one suite
  declining to measure a service the build it is running in has switched off, **and it names the
  switch**. ⚠️ It is `Assert.Ignore` rather than a pass, because a pass would claim the live
  services answered when nothing was asked.
  ⚠️⚠️ **AND THAT FILE'S OWN HEADER CLAIMED `[Category("Ugs")]` KEPT IT OUT OF THE DEFAULT RUN,
  WHICH WAS NEVER TRUE.** `CLAUDE.md` § 7's command is `!WallClock;!ThumbFloor` and has never
  carried `!Ugs`. The exclusion existed in a comment and nowhere else, which is § 5's drift rule
  inside a test file.

### 126.8c ⚠️⚠️ THE FIRST VERSION OF THE SWEEP RAN THE WHOLE SUITE AND THEN WROTE AN EMPTY `.xml`, WHICH IS A NEW INSTANCE OF § 7'S RULE

**Worth recording in full, because it is the most convincing possible argument for
`CLAUDE.md` § 7's *"always assert on the `.xml`, never on the exit code"* and it arrived from a
direction that section does not list.**

The run executed for **thirteen minutes**. The log shows scenes loading, `InputSurfaceProbe`
photographing the thumb layer, `GameplayShots` photographing a live round, and finally
`Test run completed. Exiting with code 2 (Failed). One or more tests failed.` **And the `.xml` it
wrote said `testcasecount="0" total="0" passed="0" failed="0" result="Passed"`.**

```
<test-run id="2" testcasecount="0" result="Passed" total="0" ... duration="0.4359008">
  <test-suite ... testcasecount="165" result="Passed" total="0" ... />
```

⚠️⚠️ **A FILE THAT SAYS `result="Passed"` AND `total="0"` IS THE WORST OF THE THREE STATES**, worse
than the crash § 7 already records: a crash writes no file at all and is at least visibly absent.
This one is present, well formed, and green.

**The cause was the sweep reaching something it should not have.** `SweepPersistentLeftovers`
decides an object is a candidate if **any component anywhere in its hierarchy** is a
`TumbangPreso` type, and Unity's PlayMode runner keeps its controller in the same
`DontDestroyOnLoad` scene: anything of this project's parented under it makes the whole root
match, and destroying it takes the object collecting the results with it. **Every test still
runs. There is simply nothing left to write them down.**

- ✅ **`PlayModeWorld.NeverTouch`** is the fix: a root is skipped outright if any of its components
  comes from `UnityEngine.TestTools`, `UnityEditor.TestTools`, `UnityEngine.TestRunner`,
  `Unity.PerformanceTesting` or `NUnit`. ⚠️ **It is belt as well as braces and that is deliberate.**
  The assembly filter already means nothing this project did not write can be reached, and *in
  principle* is not the standard for a method that calls `DestroyImmediate` on objects it did not
  create.
- ✅ **Verified**: `SteeringTests` alone came back **5 cases, 4 passed, 1 failed, 3.5 s** with a
  populated `.xml`, against **0 cases** before the guard.

⚠️ **AND A SECOND THING WAS WRONG WITH THAT INVESTIGATION AND IT COST A WHOLE LAUNCH.**
`-testFilter` takes a **semicolon**-separated list, not a comma-separated one. A comma-joined list
of 24 fixtures is read as one impossible name, matches nothing, and produces the same
`total="0" result="Passed"` file in thirteen seconds. **Two completely different faults with
byte-identical symptoms**, which is exactly why the rule is to read the numbers rather than the
verdict.

### 126.8d ❌ THE PERSISTENT-SCREEN SWEEP WAS BUILT, MEASURED AND WITHDRAWN, AND THE MEASUREMENT IS THE POINT

**The argument for it is sound and is still true.** Fifteen files in `Runtime/` call
`DontDestroyOnLoad` and **only six of them are services**; the other nine are SCREENS and JOBS that
need to survive one scene change (`MatchResult`, `PausePanel`, `MapPreviewSurface`, `BootSting` and
three diagnostics). A screen that survives a scene change is correct in the game, where the next
scene is the one it asked for, and wrong in a test run, where the next scene belongs to somebody
else. `PlayModeWorld.Reset`'s original note says *"those are SUPPOSED to survive"*, and that
sentence is true of `GameServices` and false of a results board.

**So it was built**: destroy every persistent root carrying a `TumbangPreso` component that is not
a named service, with an explicit refusal for anything from the test framework's own assemblies.
Then it was run on the full suite, which is the only way to find out.

| | Full run |
|---|---|
| Before this session, no reset outside five fixtures | **56 failed** |
| Reset in all 60 fixtures **plus the sweep** | **49 failed, 6 skipped** |

⚠️⚠️ **AND THE TOTAL IS THE LEAST INTERESTING NUMBER IN THAT TABLE.** With the sweep in, **every
screen suite went green** (`PlayerHubLayoutProbe` 5/5, `QueueCardLayoutProbe` 5/5,
`SettingsWheelProbe`, `LobbyStyleProbe`, `NetworkedLobbyTypingProbe`, `TrainingStreetProbe` 3/3,
`PaperPurityProbe.EveryLobbyControlSurvived`) and **eleven MATCH suites went red** that had passed
before it: *"the arena built no SliceRunner"*, *"No main camera in the arena"*, *"the match built no
slipper"*, *"the guided route did not install"*, *"NONE spawned 4 people rather than one"*.

**That is § 126.8's own definition of the thing to be afraid of, whichever direction it moves:**
*"THE COUNT BARELY MOVED AND THE RED SET LARGELY CHANGED, WHICH IS THE FINDING. Eleven suites went
green and eleven different ones went red. A gate whose red set moves is not measuring the code."*
**Trading eleven screen failures for eleven match failures is not progress, and shipping it would
have hidden the trade behind a slightly smaller total.**

- ❌ **Withdrawn**, with the whole argument kept in `PlayModeWorld.Reset` as a comment so nobody
  rebuilds it without knowing what it did.
- ⚠️ **What the right version needs is a measurement nobody has taken**: WHICH persistent object a
  match install depends on. Until that is known, destroying the set is a guess with a receipt.
- ⚠️ **The rest of the batch is untouched by this** and is measured separately: the reset pair in
  all 60 fixtures, the two scene guards, and the UGS skip are all independent of the sweep.

⚠️⚠️ **DO NOT CLOSE IT BY WIDENING A BOUND OR BY ADDING A THIRD CATEGORY EXCLUSION.** `WallClock`
and `ThumbFloor` both exist, both are documented gaps with a measured reason, and both name the
thing they exclude. A category meaning "these tests do not work next to each other" would be hiding
this finding rather than recording it.

⚠️ **`UgsServicesProbe` MAY NEED A DIFFERENT ANSWER FROM THE REST**, and it should be decided
separately: it is the only suite whose state lives on somebody else's server, so "tear the world
down" does not reach it. It is also the suite `FUTURE.md` § 0.5 rule 7 cares about most, because a
venue with no internet is the case it is really testing.

### 126.9 The four reds that are about the game rather than about the suite

These survive in isolation. All four are pre-existing on `550ba0f` and none is caused by this batch.

- ⚠️ **`CarryTests`, § 93, now on its fourth recorded sample.** 0.084, 0.092, 0.092 and 0.084 m
  against a 0.05 m bound. `CLAUDE.md` § 7.1 and § 94.8 both say do not widen it, and nothing here
  touches `Carrier`, the animator or `LateUpdate`. **The two samples § 94.8 called "not a flake"
  are four now**, which closes § 93's open question 2 about timing sensitivity: it is not a flake.
- ⚠️ **`AspectRatioProbes.TheCharacterScreenSurvivesEveryAspectRatio`: a `Label` authored at 14
  units** against the 18-unit readable floor, which is 9.3 physical pixels at 720p. `UiRows`'s
  header records three separate attempts at small text in the Godot original, each answered with
  *"text still small"*.

  ✅ **FIXED, and the receipt is that this exact label had already been "fixed" once.** It is the
  Hero picker's cooldown and charge readout (`ConvertedCharacterSelect`), and the comment above it
  says: *"CREAM AT FULL ALPHA, NOT 0.75. 🧑: 'shit down there is small and cant be seent'. This sat
  at 13 pt and three quarters opacity."* **The answer to "make it bigger" was one unit**, 13 to 14,
  on the one label on that screen carrying numbers, and the probe has been failing on it ever
  since. It is `MenuKit.MinReadableUnits` now.

  ⚠️ **AND THE BOX GREW WITH THE TYPE, WHICH IS THE HALF THAT GETS FORGOTTEN.** The 116-unit
  `minWidth` was measured against 14-unit type; at 18 the same string needs 18/14 of the room, so
  it is 150. `MenuKit.Label` is set to Overflow, so a box left at 116 would not have clipped, wrapped
  or reported anything: it would have drawn straight through the ability's name beside it. That is
  the trap `ConvertedScreen.SetHeadline` and `GameVersion.ApplyTo` each record once already.

  ⚠️ **The 13-unit key chip two lines above it is still 13** (`KeyChip`, 26x18 units, showing Q / E
  / F). It was not touched because the probe reports one label at a time and this pass fixes what
  the probe named; **run `AspectRatioProbes` again and it is the next thing you will see.** ⚠️ It
  needs more than a font bump: the chip is 26 units wide and `Hud.KeyLabelFor` returns
  `BUTTON WEST` on a pad, so raising the type without solving the pad label is trading one
  overflow for a worse one. That is the same authored-glyph gap § 125.13 leaves open.
- ⚠️⚠️ **`ModelPreviewTests.HeroCharacterSelectShowsAbilitiesInsteadOfClassicAttributes`, AND IT IS
  A REAL DESIGN FAULT RATHER THAN A TEST THAT NEEDS UPDATING.** The Hero picker draws
  `Seismic Stomp` and `Demonic Carapace` in title case beside `TITAN FISSURE` in upper case, **in
  one three-row panel**. The cause is honest and § 124's own work: the two skill rows show the
  EQUIPPED VARIANT's name (`HeroLoadoutRules`'s `VariantName`, written in title case: *Long
  Tremor*, *Black Ice*) while the ultimate has no variant and falls back to the ability's own name
  (`DanteHeroKit`'s `"TITAN FISSURE"`, upper). Every other surface in the game
  (`AbilityIcons`, `CustomCharacterScreen`) writes an ability upper. **So the picker ships two
  visual languages in one panel**, which is `CLAUDE.md` § 6.5's complaint exactly, and it does it
  hardest on the DEFAULT loadout, where the variant's name is the ability's name in the wrong case.
  ✅ **FIXED, and in the data rather than on the screen.** The picker's own note already stated
  the invariant it depends on: *"they are the same string on a default build, so nothing moves for
  a fresh account and everything is correct for one that has equipped anything."* **They were not
  the same string, and nothing checked.** All 24 `AbilityVariant` names are upper now, which is
  what every other surface in the game already writes (`AbilityIcons`, the `HeroKit` constructors,
  `CustomCharacterScreen`), so the picker draws one voice and the change reaches the loadout board
  and the lobby nameplates for free rather than only the one screen that was photographed.
  **`Core.Tests/AbilityNamingTests` is the check**, in the core rather than in a UI test, because a
  naming convention only a screen enforces is a convention the next screen breaks.

  ⚠️⚠️ **AND THE TEST IMMEDIATELY FOUND A SECOND ONE THAT WAS NOT A CASE PROBLEM AT ALL.**
  Phaister's slot 1 default variant was named **`HEX SIGIL`** while the ability it is a reading of
  is named **`HEX`** (`PhaisterHeroKit`, and `BaseAbility` in the same table row). Three names for
  one power, in three files, with `AbilityIcons` casting the deciding vote for the wrong one. It is
  `HEX` now, which is what the two independent sources already agreed on. ⚠️ **The test does not
  pick the name, it requires that they agree**: renaming the power to `HEX SIGIL` is still open to
  anybody who wants it, and now they have to change the kit and the table together.
- ⚠️ **`SteeringTests.MouseAimedMovementIsRelativeToTheBody`, a 2.6 per cent near miss**: facing
  east, W moved the seat 1.973 against a 2.0246 bound. It is close enough that it could be a tight
  bound rather than a defect, and § 34 is the entry about body-relative steering that would say
  which. **Do not widen it without reading § 34 first**: that section exists because seat 0 once
  travelled 224 m against 522 to 556 for its siblings, and the symptom was exactly this axis.

### 126.10 The .apk was built, installed and run for the first time, and two of its settings were never taking

`docs/FUTURE.md` § 15 step 1 says *"nothing else here means anything until that has happened
once"*, and § 125.13 recorded it as never done. **It is done.**

```
[Build] SUCCEEDED. 1899 MB, 1631s -> C:\Users\Matthew\Desktop\TumbangPreso-Android\TumbangPreso.apk
adb install -r  ->  Success  (72 s, 235 MB)
ApplicationInfo 'com.bhstudios.tumbangpreso', Version '1.0.0', Min API Level '26', Target API Level '36'
Scripting Backend 'il2cpp', CPU 'arm64-v8a', Stripping 'Enabled'
Device Model 'Google sdk_gphone64_x86_64', OS 'Android OS 14 (API 34)'
[Audio] loaded 123 of 123 cues.
TumbangPreso.UI.BootSting:Play()
```

Evidence is in `Logs/shots-android/`: `logcat-unity-v1.txt` and two screencaps. The game boots,
loads every audio cue, plays the boot sting and reaches its own **preparing shaders** screen.

⚠️⚠️ **AND READING THAT LOG FOUND TWO SETTINGS `ConfigureAndroid` WRITES THAT THE ENGINE DOES NOT
ACCEPT. BOTH ARE THE SAME FAULT AS `CLAUDE.md` § 6.4's `ConfigureSplash`, FROM THE OTHER SIDE:
the write is in code, on every build, and it still did not take.**

1. **`minSdkVersion` 24 is REFUSED.** The build printed, as a `Debug.LogError` out of the exact
   line: *"Minimum supported Android API level is 26 (Android 8.0 Oreo). Please use
   AndroidApiLevel26 or higher."* and then carried on and shipped a player whose manifest says
   26. **The file said 24 for a documented reason and the .apk never once had it.** It is 26 now
   and the original reasoning is kept beside it: the floor moved by one year of phones and the
   argument survives that.
2. ⚠️⚠️ **THE x86_64 SLICE IS NOT IN THE .apk, AND THE COMMENT ABOVE IT INSISTS IT IS THE WHOLE
   REASON THE BUILD IS TESTABLE.** `PlayerSettings.Android.targetArchitectures` is set to
   `ARM64 | X86_64` and the shipped file contains **arm64-v8a only**:

   ```
   arm64-v8a        7 files     119.9 MB
   ```

   The comment reads *"x86_64 is not optional here: 🧑 has no Android handset, so an ARM64-only
   .apk could not be run by anybody on this team."* **The .apk IS ARM64-only and it ran anyway**,
   because the Android 14 x86_64 system image translates arm64: the log says
   `CPU 'arm64-v8a'` on a device whose `ro.product.cpu.abi` is `x86_64`, and the loader picked
   `lib/arm64-v8a` out of the package. So the claim was false and the conclusion it protected was
   also false, in opposite directions, and they cancelled out. **Done looks like:** the comment
   rewritten around the measured fact (the emulator translates), and a decision about whether to
   keep asking for a slice Unity 6 does not emit.

⚠️ **THE EMULATOR IS A 1-CORE, GPU-DISABLED, TRANSLATING DEVICE AND IS NOT A PERFORMANCE
MEASUREMENT.** `SystemInfo CPU = x86-64, Cores = 1, Memory = 2474mb`, `hw.gpu.enabled = no` in
the AVD, and every native instruction translated from ARM. **`FUTURE.md` § 15 item 3 (performance
on device) cannot be answered here**, and a number taken from this emulator would be worse than no
number.

⚠️⚠️ **BUT IT DID FIND ONE REAL THING, AND IT IS THE FIRST THING A PHONE MEETS.** The app never
got past its own **"preparing shaders"** bar in two separate launches, several minutes each, and
Android raised its "isn't responding" dialog over the loading screen twice. The cause is one line:

```csharp
SetLoadingStage("preparing shaders", 0.04f);
yield return null;
Shader.WarmupAllShaders();      // SplashScreen.PreloadGameAssets, stage 1
```

**`Shader.WarmupAllShaders()` compiles every variant in the build in a single blocking call**, and
it is the ONE stage in that whole routine that cannot yield. ⚠️ **The method's own header already
states the rule it breaks**: *"IT YIELDS BETWEEN EVERY STAGE, DELIBERATELY. This runs while a video
is playing; a stage that blocks for 400 ms stutters the sting itself"*, and every other stage was
carefully broken up per character for exactly that reason (§ 114.4). This one was left whole
because on a desktop it costs a few seconds.

**Done looks like:** a `ShaderVariantCollection` warmed a slice per frame, so the bar moves and the
OS never sees a frozen main thread. ⚠️ **It is not only an emulator problem**: a cheap Metro Manila
handset is the target (`ConfigureAndroid`'s own note) and it will pay a version of this cost on
every cold boot, on the one screen where a player has nothing to look at but a bar that is not
moving. ⚠️ **And an ANR at boot is the worst place to have one**, because Android offers the player
a button that closes the game.

⚠️ **AND A WARNING WORTH ACTING ON BEFORE A REAL PHONE:** *"PlayerSettings->Active Input Handling
is set to Both, this is unsupported on Android and might cause issues with input and application
performance."* `activeInputHandler: 2` is what `FUTURE.md` § 14 records as the reason
`StandaloneInputModule` ran without erroring while no pad binding could reach it. Moving to the
new system alone would break the legacy `Input.GetKeyDown(KeyCode.Escape)` calls still in
`ConvertedSettingsPanel.Update` and elsewhere, so **it is a change with a real blast radius and it
should be made deliberately, on a device, not on the way past.**

### 126.11 ⚠️ NOT DONE: crossplay is still argued rather than demonstrated

§ 125.13's bullet stands, narrowed. Both halves now exist (a Windows player and an .apk from one
tree) and `NetSession.ProtocolVersion` is untouched at 21, so the claim is stronger than it was.
**Nobody has still watched them join each other.** What this session learned that the next one
needs:

- ⚠️ **The .apk has no UGS session on the device.** The first launch logged *"[Social] presence
  not written: Cloud Code is unavailable: no project id or no signed-in session."* A join by code
  goes through UGS Relay, so that has to resolve first.
- ⚠️ **`UgsServicesProbe` went red six times in one full PlayMode run** with *"You are not signed
  in to the Authentication Service"* and *"The player is already signing in"*, having passed in
  the run an hour before. **Whatever that is, it sits between here and a crossplay demo.**
- ⚠️ **The emulator is fragile and the AVD does not survive a hard kill.** Boot once, leave it
  alone, and never `Stop-Process -Force` it: three relaunches after one forced kill hung at the
  same point, and the emulator's own log showed `Failed to find ColorBuffer` and
  `Failed to load opengl32sw` before it crashed. `-wipe-data` did not clear it; a reboot did.

### 126.12 · Verified

- **Core 433/433** (`dotnet test`, 93 ms). 431 plus the two new `AbilityNamingTests`. ⚠️ **Both new
  tests were watched failing first**, on `Seismic Stomp` and then on Phaister's `HEX SIGIL`, which
  is the only way to know a test can fail at all: `Phase10Tests`'s own header records the version
  of itself that *"compared a constant to itself"* and could not.
- **EditMode 295/295** (6 s). 292 plus the three `QualitySettingsAssetTests`.
- ⚠️⚠️ **THE THUMB FLOOR: 1519 SHORTFALLS TO 50, AND THE SWEEP IS COMPLETE RATHER THAN SHORTER.**
  `InputSurfaceProbe` run **alone**: 5 of 6 pass, and the one red is
  `TheFrontEndMeetsTheThumbFloor` with **50 controls** across twelve shapes. The report also shows
  **12 scrollbars exempted** and **zero** *"the camera was replaced part way through the sweep"*
  lines, so this is a full measurement and not a truncated one. ⚠️ **The first run after the fix
  reported 36 and that number was worthless**: the settings panel had destroyed itself on open and
  three scenes had been cut short by the old camera guard, so it measured less and said less.
  **Compare reports by what they covered, not by the count at the bottom.**
- ⚠️⚠️ **AND `EveryScreenHasAFocusPathAndReachableTouchTargets` PASSES, WHICH IS THE HALF THAT
  MATTERS.** That is check 3, *"a press at a control's centre must land on that control"*, and it
  is the check that caught the padding bug in § 125.4. **Making forty rows taller on touch stole
  no presses.** A pass there is worth more than the shortfall count.
- **What the remaining 50 are**, and they are near misses rather than the old class of failure:
  `MainMenuCanvas/QuitButton` at **676x141** (three units short of the floor),
  `LobbyTopRail/BackButton` at **124x144** (twenty short on WIDTH, which is the axis the make-room
  pass deliberately does not touch), and the lobby tab bar at 137x168. **Done looks like** a width
  answer for the lobby rail and three units on the menu pennants; the height problem is solved.
- **Full PlayMode, `!WallClock;!ThumbFloor`: 42 red, then 41 red after the § 126.1 fixes**, with
  eleven suites swapping sides. § 126.8 is that finding and it is the reason there is no honest
  pass count for this suite today.
- ⚠️ **The nine suites carrying about twenty of those failures: 29 of 31 pass when run together on
  their own**, in 105 s. The two survivors are § 126.9's third and fourth bullets.
- ⚠️⚠️ **A TWELVE-SUITE RUN WAS 15 OF 38, AND THAT IS § 126.8 AGAIN RATHER THAN A REGRESSION.**
  Adding `InputSurfaceProbe` to the group is what did it: it loads every scene in the build
  settings and opens every overlay it can discover, so it is the most destructive fixture in the
  suite and everything after it inherits the wreckage. **`InputSurfaceProbe` is run on its own**,
  and the numbers above are from that run.
- **Android: `[Build] SUCCEEDED. 1899 MB, 1631s`**, installed in 72 s, launched, logcat and two
  screencaps in `Logs/shots-android/`. § 126.10.
- ⚠️ **`ProjectSettings/QualitySettings.asset` stayed clean through every one of these runs**,
  which is the measurement § 126.4 rests on.
- ⚠️ **`NetSession.ProtocolVersion` is untouched at 21.**
  `InputContractTests.TheInputPassDidNotMoveTheProtocolVersion` is green, so the .apk and the
  Windows player from this commit will accept each other.

### 126.13 ⚠️ OPEN: what THAT batch did NOT do (the `Fit` floors are resolved in § 130.11)

- ⚠️ **Crossplay is still not demonstrated.** § 126.11 has what the next session needs.
- ⚠️ **`AspectRatioProbes.TheCharacterScreenSurvivesEveryAspectRatio` is still red**, and the
  authored 14-unit label § 126.9 fixed was not the only source of a 14. Three
  `MenuKit.Fit(label, room, 14)` calls in `ConvertedCharacterSelect` (lines 592, 1094, 1205) pass
  **14 as the shrink floor**, so a label that does not fit is allowed down to 14 and the probe
  cannot tell that from an authored 14. ⚠️⚠️ **The comment above the first one says
  *"14 AS THE FLOOR RATHER THAN 18, AND ONLY HERE"* and there are three of them**, which is a local
  exemption that was copied twice and never encoded anywhere a test could see. **Done looks like** a
  decision, taken with a render in hand: either the pills get wider and the floor goes to
  `MenuKit.MinReadableUnits`, or the exemption is written into the probe by name so it stops being
  a permanent red. **Do not lower the probe's floor to make it green.**
- ⚠️ **Pad glyphs are still words.** `Hud.KeyLabelFor` answers `BUTTON WEST`, not a face-button
  glyph, and the Hero picker's 26-unit key chip cannot hold either. § 125.13's last row; it needs
  authored art.
- ⚠️ **On-device performance is still unmeasured**, and § 126.10 says why the emulator cannot
  answer it.
- ⚠️ **`UiClickProbe` still carries its hard-coded five-screen list**, which is § 124.11's fault
  pre-installed. Untouched deliberately.

---

## 121 · The v61 report: one material for the primaries, a hub with a tab column, and the stuck hover ⚠️⚠️ OPEN, 2026-09-02, branch `ui-redesign`

🧑 opened the build off `d7731070` and sent fourteen notes in one sitting, with a crop for
almost every one. He also said how he wanted them handled: **"thorouhly plan how to fix
everything btw"**, *"dont js shit out fixes"*, *"dont worry abt the stuff im liosting, im js
listing them"*, and **"i want u to think abt color and visual harmony in makingh fixes"**.

⚠️⚠️ **THIS ENTRY IS THE PLAN AND IT IS WRITTEN BEFORE THE WORK, WHICH IS THE ORDER
`CLAUDE.md` § 6.2a ASKS FOR AND THE ONE § 118.2 NAMES: run `game-ui-design` as a CRITIC first.**
Every row below has a cause in a named file and, where a colour or a distance is claimed, a
number measured off a render rather than an adjective.

⚠️ **`tools/sample_png.js` IS NEW AND IT IS WHY THE NUMBERS EXIST.** Every colour argument in
this file (§ 119.1's road sampling, § 119.10's 1.7:1, § 120.6's channel split of `TUMP.png`) was
produced by a tool that was not in this repository, so no reader could re-run one. It decodes a
PNG with `zlib` and nothing else, because `python` was not on PATH on the machine it was written
on, and it prints pixels, scan lines, the commonest colours in a box, and the WCAG ratio between
two hexes.

⚠️ **THAT REASON IS PROFILE-SPECIFIC AND WAS WRITTEN AS THOUGH IT WERE UNIVERSAL.** On the
`Matthew` profile `python` IS on PATH (checked 2026-09-04: both `Python312\` and its `Scripts\` are
in the USER Path, and `python --version` answers from PowerShell and from bash). There are two
laptops with two profiles, `CLAUDE.md` § 7.1 now says so rather than asserting either, and **the
tool stays regardless**: a Node decoder that needs nothing installed is the more portable answer
across two machines, which is a better argument for it than the one originally given.

---

### 121.1 ⚠️⚠️ THE ONE FINDING THAT EXPLAINS FIVE OF HIS NOTES: THE PRIMARY IS THE ONLY CONTROL IN THE PAPER FRONT END STILL DRAWN IN WOOD, AND ITS SHADOW IS GREY

🧑, on four different screens, without connecting them himself:

| Screen | What he said |
|---|---|
| Lobby | *"orange outline when i hover over start match is ugly"*, then **"u really have to redesign start match button, it doesnt FEEL like a start match button"**, then the correction that matters: **"i like the size adn color but it feells so flat, it doesn thave start match energy"** |
| Character maker | *"js make buttons prettier"*, **"bcz i dont get why theres rounded sshit next to square shit or wtbv the design of the shit nexxt to it is"** (BACK, a paper pill, beside KEEP AND USE, a chamfered green slab) |
| Character select | *"these buttons look ugly"* (the four-tab row, three surfaces in one rail) |

**Measured off `Logs/shots-runtime/SignInCreate-v56.png` with `tools/sample_png.js`:**

| What | The pixel | Hue | Saturation | Value |
|---|---|---|---|---|
| The cream field | `f4ecdd` | 39 | 9 % | 96 % |
| **A paper control's edge** (`PLAY AS GUEST`, x=227) | `dcc19a` | 35 | **30 %** | 86 % |
| **The green primary's edge** (`CREATE ACCOUNT`, x=161) | `ada69b` | 37 | **10 %** | **68 %** |

⚠️⚠️ **SAME HUE, A THIRD OF THE CHROMA, EIGHTEEN VALUE STEPS DARKER. That is the "grey" he can
see and cannot name**, and it is `CLAUDE.md` § 6.4's rule caught on the warm axis rather than the
blue one: the section forbids *"cold grey"* and a 10 per cent saturation neutral beside a 30 per
cent warm edge is exactly that, whatever its hue reads as in a hex.

**And the silhouette is the other half.** `WoodCraft.Surface.Action` is chamfered and every paper
control is a pill or an 18-unit round, so on the maker's footer a rounded cream pill stands beside
a chamfered green slab with a grey halo. 🧑 named the silhouette clash before anybody measured the
colour, which is § 6.5's *"A CHAMFER MEANS PRESSABLE AND A ROUND MEANS FURNITURE"* read back at us
from the outside: **the chamfer is now the odd one out, because everything else on these screens
became paper and the primary did not.**

⚠️⚠️ **AND "FLAT" IS A THIRD, SEPARATE FAULT ON THE SAME OBJECT, WHICH IS WHY HE SAID IT AFTER
SAYING HE LIKED THE COLOUR.** § 120.1 gave every paper control an eased hover and press:
`PaperButton` lifts the face two units, scales the object 2.5 per cent, sinks the lettering by
`Drop` and takes the cast shadow off, all in unscaled time and faster down than up.
**`LobbyChrome.BuildActionSlot` disables `ArrowButtonView` on the primary and attaches no
`PaperButton`, so the one control on the screen that most needs to feel pressable is the only one
in the front end with no motion at all.** It has `GodotButton`'s sprite swap and Godot's five-unit
label sink and nothing else. *"it feells so flat"* is a measurement of that gap.

**What done looks like:**

1. ⚠️ **`PaperCraft.Surface.Action`: one new construction, in paper's own language.** A raised
   slab built by `PaintRaised`'s rules (lit top edge, 14 per cent ramped wall, squared-falloff
   cast shadow) at primary weight, with the corner radius of a `Token` rather than a chamfer, and
   **a warm shadow derived from the fill rather than a neutral**. The shadow must sit at or above
   the 30 per cent saturation every other paper edge on the screen carries, which is the number in
   the table above and is the whole of what "harmony" means here.
2. ⚠️⚠️ **THE FILL IS A PARAMETER AND § 6.5 SAYS THAT IS HOW SCREENS BECOME ONE SCREEN, SO READ
   WHY THIS IS THE EXCEPTION.** That rule forbids a fill being the ONLY difference between two
   ROLES. There is one role here, `Action`, it appears once per screen by construction, and the
   two fills are both authored: 🧑's green (`UiTheme.MenuGreenFace`, the measured peak of
   `JOIN BUTTON.png`) and the lobby's brown, which he asked to keep by name (*"i like the size adn
   color"*, and § 119.10's *"u can also still use the brown color ... start match lowk looks
   good"*). **Two authored fills, one construction, one per screen.**
3. **Every primary in the paper front end takes it**: `StartButton` / `PrimaryButton` (lobby),
   `KEEP AND USE` (maker), `CHOOSE` (picker), `CREATE ACCOUNT` / `SIGN IN` (login), the hub's
   footer action. ⚠️ **The main menu keeps `WoodCraft` untouched**, which is the scope line § 119
   draws and the reason `WoodCraft` is not being edited.
4. **`PaperButton` goes on all of them**, which is the motion. ⚠️ **`GodotButton` already owns the
   label's position on these nodes** (its own five-unit sink), so `PaperButton` must not write the
   label offset while a live `GodotButton` is present: two owners of one transform property is
   § 119.9 row 1 and it has already shipped once.

---

### 121.2 ⚠️⚠️ THE STUCK HOVER, AND IT IS A CACHE KEY WITH ONE FIELD MISSING ✅ CAUSE FOUND

🧑, with a crop of the lobby's mode tabs, one lit brown and one outlined: **"theres brown ink left
over if i dont hover back to the buttons on top"**, *"i like it but make it so that i dont have to
hover back to buttons on top to get rid of it"*.

**`PaperSkin.Rebuild` keyed its cache on the rect height and the SURFACE and not on the POSE:**

```
if (_built > 0.0f && Mathf.Abs(height - _built) < 2.0f && _builtSurface == Surface) return;
```

`SetPose` clears `_built` to force the repaint, and **`Rebuild` returns without painting and
without recording anything when the rect reports zero height**, which is every frame the control
is inactive. A drawer that closes over a hovered chip is exactly that: the pose write is dropped,
`_pose` still says `Hover`, and the next `OnEnable` repaints from it. **The plate comes back lit,
on a control nothing is pointing at, and only a fresh enter-and-exit clears it.**

⚠️ **THE SURFACE WAS IN THE KEY AND THE POSE WAS NOT, WHICH IS WHY IT LOOKED LIKE A COLOUR BUG.**
`PaperKit.MarkLive` swapping `Live` for `Ghost` always repainted, so the tab row's SELECTION was
never wrong. Only its lighting was, which is why "brown ink left over" is a better description of
it than anything the code says.

**The fix, and it is two lines plus a guard:**

- `PaperSkin._builtPose` joins the cache key, so a pose written against a zero-height rect is
  re-applied the moment the rect exists instead of being forgotten.
- `PaperButton.OnDisable` already resets `_hovered`, `_held`, the scale and the label. **It must
  also put the SKIN back to `Rest`**, because that method's own header says *"a control that is
  switched off mid-hover never gets its `OnPointerExit`"* and it was fixing the transform half of
  that and not the surface half.
- `PaperButton.OnEnable` re-asserts the pose, so a control that was disabled by something other
  than a pointer (a tab rebuild, a drawer) cannot come back mid-animation.

---

### 121.2b ⚠️⚠️ THE AMBER FOCUS RING, WHICH HE REJECTED THREE TIMES IN ONE SITTING ON THREE DIFFERENT CONTROLS

🧑, in order: *"orange outline when i hover over start match is ugly"*, then, with a crop of the
login screen's USERNAME box, **"i dont like the orange outline for a lot of things"**, then the
question that settles the design rather than the colour: **"why do we even have an orange outline
when we hover or select stuff"**.

**It is the keyboard and controller focus indicator, and it was doing two jobs badly.**

| What | The measurement | What it becomes |
|---|---|---|
| It lit on POINTER HOVER as well as on focus | § 120.1 already gives every paper control an eased hover: the face lifts two units, the object scales 2.5 per cent and the cast shadow grows. **A second, louder hover indicator on top of a hover indicator** is `CLAUDE.md` § 6.2 question 3 answered wrongly. | Hover is the pose. The ring lights for real focus only, which is `game-ui-design`'s `missing-focus-visible` and its controller-navigation pattern, neither of which emits a pointer event. |
| It was `UiTheme.Amber` | `ffba00` on `Paper` `f4ecdd` is **1.46:1** (`tools/sample_png.js contrast`). **A high-chroma shape carrying almost no value difference from the sheet under it**: it shouts and it does not read, which is the worst pair of properties a focus indicator can have. | `UiTheme.WoodMid`, which measures **9.20:1** on the same sheet and adds no colour that is not already this front end's ink. |
| It drew a rounded-rect ring around a chamfered slab | Two silhouettes, so the "outline" was a box near a button rather than an edge on one. | Moot on the primary, which is a pill in the same family as everything else now (§ 121.1). |

⚠️⚠️ **IT IS THE SAME INVERSION `PaperCraft.Surface.Live` AND `Surface.Sign` BOTH ALREADY MADE,
ARRIVING ONE CONTROL LATER.** § 118.4 says *amber is the marker*; that rule was written for a
WOODEN front end where amber was the one LIGHT thing on a dark screen. **Invert the field and the
rule inverts with it: on cream the marker is the one DARK thing.** § 119.10 records him rejecting
that ratio by eye on two other controls before this one.

⚠️ **DELETING THE COMPONENT WAS THE WRONG ANSWER AND IT WAS TEMPTING.** A text field has no pose
of its own (`Surface.Tray` is a recess in every state), so without a focus mark there is nothing
at all saying which of two boxes your typing goes into. This is a narrowing, not a removal.

---

### 121.2c The wordmark on the login card: half its own sign was empty brown

🧑: **"improve tump logo integration in lobby too, I like the current setup but it doesnt have
much impact, especially wiht a brown button thats empty like taht"**.

⚠️⚠️ **THE OLD CODE'S OWN COMMENT IS THE CONFESSION AND NOBODY HAD MULTIPLIED IT OUT.** It read:
*"at 420 wide less 2 x 26 of inset the mark is 368 wide and about 106 tall, and 120 units of
plaque less the inset and the six-unit shadow leaves it 62. So the fit is decided by HEIGHT and
the mark draws about 216 x 62 in the middle of the plaque."* **216 of 420 is 51 per cent.** The
game's name occupied half of its own sign and the rest was bare wood, which is exactly what he
photographed.

**A fitter whose box is a different shape from the thing in it always spends the difference as
margin.** `TUMP.png` is 1835x527 (3.482:1) and the box was 368x62 (5.9:1), so the mark was pinned
by height and the width ran away. The fix is to size the MARK and derive the plaque from it, so
the box and the mark are the same shape and the fitter has no slack to spend: **336 units wide
against 216, a little over half again, bought by deleting empty wood rather than by growing the
sign.**

⚠️ **The plaque grew 26 units taller, so `Logo` moves up 13 to hold the 36-unit gap under it.**
That gap is the number he asked for after *"this part looks too tight"* (§ 119.10), and letting it
close to 23 would have put the identity block back on top of the form block.

---

### 121.3 The lobby, five notes

| # | 🧑's words | The cause | What done looks like |
|---|---|---|---|
| 1 | *"these look ugly"*, *"it looks ugly bcz it isnt centered like both of them and theres big empty space"* (the DANTE and SKILLS rows) | **The SKILLS pair is not centred and DANTE is.** `LobbyChrome.BuildSkillsRow` gives the caption a box ending 10 units left of the row's middle and the value a box STARTING at the middle, left-aligned. So the pair spans from `centre - captionWidth - 10` to `centre + valueWidth`: `SKILLS` is about 62 units and `Standard Build` about 130, which puts the pair's own centre **34 units right of the row's**. `BuildCharacterRow` centres `DANTE` properly, so the two rows in one column have two different centre lines. | The caption and the value become ONE centred object (a horizontal group that sizes to its content and centres as a unit), so the pair's centre IS the row's centre and cannot drift when either string changes. ⚠️ The two boxes must still not overlap, which is why they share an edge today; a layout group gives the same guarantee without the arithmetic. |
| 2 | *"also make everything centered (your tier unranked looks ugly bcz it isnt centered"* | `LobbyChrome.BuildTierPlate` draws all three lines `UpperLeft` / `MiddleLeft` / `LowerLeft` while the fighter column beside it and the mode plate above it are centred. **One plate on the rail is aligned differently from every other.** | All three lines centre. ⚠️ The note wraps to two lines and its box is already measured at 64 units for exactly that (see that method); centring changes the alignment and must not change the height. |
| 3 | *"orange outline when i hover over start match is ugly"* | `FocusRing` lights on POINTER HOVER as well as on real focus, and it draws `UiMaterials.Ring(Amber)`, a **rounded-rect** outline, around a **chamfered** slab. Two faults: a silhouette that does not follow the control, and an accent spent on hovering. Amber on `Paper` measures **1.46:1** (`sample_png.js contrast ffba00 f4ecdd`), which is under even the 1.7 § 119.10 records him rejecting by eye. | The ring stops reacting to the pointer and lights only for keyboard and controller focus, which is what `game-ui-design`'s `missing-focus-visible` actually asks for; hover is already said by the pose (§ 121.1 item 4). ⚠️ **The input fields keep their ring**, because a focused text field has no other state and that is the one place the amber outline reads well in his own screenshot. |
| 4 | **"Taya first is ugly and unreadable, too much empty space too"**, *"maybe tighten its box and add outline to Taya first or smth (its okay if player you and taya first boxes doesnt match), js keep everything centered still"*, and **"ALSO i want taya first to be ABOVE the player you, instead of it being button"** | `LobbyNameplates` draws TAYA FIRST as a full-width bar UNDER the name plate, in the same family as a pressable chip. **It is a badge and it is drawn as a button**, which is `CLAUDE.md` § 6.3's *"one that does nothing must not look pressable"* the wrong way round, and it is below the thing it qualifies. | The badge moves ABOVE the plate, sizes to its own lettering rather than to the plate's width, gets a keyline so it reads as a stamp rather than a slab, and keeps its text centred. ⚠️ **It stops sharing the plate's width on purpose and he said so**: *"its okay if player you and taya first boxes doesnt match"*. |
| 5 | **"chat doesnt work at all btw"**, with the drawer open, an empty log and a live `Say something` field | ⚠️ **NOT DIAGNOSED YET AND IT MUST NOT BE GUESSED AT.** § 79.3 has had *"THE LOBBY CHAT STRIP SHOWS NOTHING"* open since 2026-08-29 and this is either that or a second fault on top of it. `LobbyChat.Submit` routes to `MatchRpc.Instance.SendChatServerRpc`, so the first question is whether `MatchRpc.Instance` is non-null in a lobby that has auto-hosted, and the second is whether anything writes the local line when it is null. **Reproduce it in the running player first**, single machine, then hosted. |

---

⚠️⚠️ **AND THE CHAT IS TWO SEPARATE COMPLAINTS, WHICH IS WHY IT IS NOT ONE ROW.** Besides
**"chat doesnt work at all btw"** he sent, of the same drawer, **"also chat is awkwardly placed,
it looks very ugly"**. One is a function that does not run and one is a composition; fixing either
does not touch the other, and shipping the second without the first would be a beautiful drawer
that still does nothing. **Reproduce the function fault in the running player first**, because
§ 79.3 has had *"THE LOBBY CHAT STRIP SHOWS NOTHING"* open since 2026-08-29 and this is either
that entry or a second fault standing on it. His crop shows the well empty and the `Say something`
field live, which is the same picture § 79.3 describes.

⚠️ **The placement half has a measurement already**: § 118.1 row 1 records the well as *"about 70
units tall to hold one 18-unit line, and the line sits at its BOTTOM because lines fill upward, so
two thirds of it is empty by construction"*. That was written before the drawer existed and the
arithmetic survived the move into it.

---

### 121.4 The login screen, two notes, and the first is not what it looks like

- **"sign in isnt centered"**, with a crop of the two tabs. ⚠️⚠️ **MEASURED ON `SignIn-v56.png`
  AND IT IS CENTRED THERE, WHICH MEANS THE MEASUREMENT HAS TO BE RETAKEN ON THE BUILD HE IS
  ACTUALLY LOOKING AT.** The pill spans x 176 to 365 (centre **270.5**) and the lettering's ink
  spans 242 to 300 (centre **271**): half a pixel. **But v56 predates § 120.3**, which moved
  `SignInScreen.SetTab` onto `PaperKit.MarkLive` and therefore changed the idle tab from `Token`
  to `Ghost` and the live one to `Live` at a different height (`LiveTabHeight` 60 against
  `IdleTabHeight` 52). His crop shows the `Live`/`Ghost` pair, so it is the newer geometry.
  **The first action is a fresh shot at `v61` and the same two scans**, not a fix.
  ⚠️ `PaperKit.CentreOnFace` is the likely suspect and § 120.2 is the receipt for why: two
  correction sites that move a label six units in opposite directions already shipped once on
  BACK, and a `Live` surface has a `Drop` a `Ghost` does not.
- **"ugly ass empty space here"**, *"cant u js use the left side as space like"*, **"like use this
  whole space for login"**, with the card cropped and then a second crop of the card enlarged into
  the space beside it. The card is **557 units wide against a 1920-unit canvas**, sitting at
  `CardMargin` 96 from the left edge, and § 120.6 has just sized its HEIGHT to its content while
  leaving its WIDTH at `ColumnUnits` 560. So the form is a narrow strip with the cast beside it,
  and inside the strip the pitch between blocks is 120 units, which is where the vertical holes he
  is pointing at come from.
  ⚠️⚠️ **THIS REVERSES § 100 AND THE REVERSAL IS HIS, SO READ § 100 BEFORE TOUCHING IT.** That
  entry cut the column from 38 per cent of the window to 580 units *"which is the form plus one
  margin either side"*, because 860 units of wood around a 420-unit form was swallowing the key
  art. **The answer is not to stretch the form back out to 1200**, which is § 94.7 fault 6 exactly
  (the widest control becomes the loudest thing on the screen). It is to give the wider card
  something to PUT there: the wordmark and one line of purpose on one side, the tabs, the two
  fields and the primary on the other, so the space is filled by content rather than by a
  stretched text box.
  ⚠️ **And the key art must be re-fitted in the same commit.** § 6.2c question 2: the picture's
  frame ends where the opaque thing starts, and `BuildKeyArt` currently envelopes the WHOLE canvas
  on the argument that the card floats over it. A card that takes half the screen makes that
  argument false again, which is the exact fault § 100 recorded as *"the art is cut off"*.

---

### 121.5 The character select, three notes, and one of them is the blue arrows

- **"these buttons look ugly"** (the `HERO` / `LATA` / `TSINELAS` / `MAKE YOUR OWN` row). Three
  surfaces in one rail: `HERO` is a `Live` pill, `LATA` and `TSINELAS` are `Ghost` outlines, and
  `MAKE YOUR OWN` is a filled `Token` with no outline, at a smaller type size because it is the
  longest string and `MenuKit.Fit` shrank it. **Four controls that do the same kind of thing must
  look the same** (§ 117's whole complaint), and the fourth is not even the same KIND: it is a
  door out of this screen sitting in a row of tabs within it.
- **"this looks ugly"**, *"i think it can be improved by using diff background"*, **"this used to
  be amazing when it was brown only and the background corresponded to their color"**, then, of
  the version that does tint: *"yea see this doesnt look great"* (NEMU, whose wash is purple).
  ⚠️⚠️ **BOTH NOTES ARE TRUE AND THEY ARE THE WHOLE COLOUR PROBLEM ON THIS SCREEN.** He wants the
  backdrop to respond to the character; he does not want the character's own hue painted across a
  cream sheet, because six heroes' colours include a purple and a magenta and **the front end has
  four hues in it on purpose** (§ 119.1, § 118.4: *"do not add a fifth hue"*). The resolution has
  to be a treatment that varies by character WITHOUT importing an arbitrary hue: vary the
  backdrop's VALUE and its warmth within the paper family and let the character's colour appear as
  a low, contained glow behind the model rather than as a full-screen wash. **Value is an ordering
  tool and hue is the last one**, which is the same inversion § 119.10 records for amber.
- ⚠️⚠️ **THE ARROWS ARE BLUE AND THEY ARE NOW ON A CREAM SCREEN, WHICH IS THE CONTEXT § 120.7
  SAID TO LOOK AT.** Measured on `Assets/TumbangPreso/Art/ui/host-game/Arrow Left 64.png` (which
  is 27x39, not 64x64): of the opaque pixels, about **70 per cent `ffffff`** and about **30 per
  cent the `80bad9` family, hue 201 at 41 per cent saturation.** Against a field at hue 39 they
  are the only cool object in the front end, and they sit beside `DANTE` in his own green.
  **Both of his rules still disagree and § 120.7 is still right that only he settles it, but there
  is now a third option that was not on the table then**: the same decision the lobby already made
  twice, which is to stop DRAWING one of his files on the paper screens while leaving the file,
  the main menu and every other use of it untouched. A warm arrow drawn from his measured
  silhouette is not a repaint of his art; it is the treatment `ConfigPanel` and `MAP MODE DISPLAY`
  already got in § 120.4. ⚠️ **Do not runtime-tint the PNG**, which § 120.7 rules out by name and
  which would multiply his white down to tan and his blue to mud.

---

### 121.6 The player hub, which is § 120.7's own open item and the largest piece

⚠️ **THIS IS NOT ONE OF HIS NOTES. It is what § 120.7 named as unfinished** and what § 119.5
planned from the start: *"an ID card with a tab COLUMN rather than a tab row ... six tabs across a
header is the row that made § 92 unreadable"*.

Measured off `LobbyAccount-v56.png`, and the shape of it survives § 120's repaint: six 168-unit
tabs run across the screen at y = -182, the list starts 232 units below the top, and **on the
PROFILE tab of a fresh account the bottom 45 per cent of the screen is bare cream**. That is
§ 6.2's *"big ass empty sopace"* on the one screen in the game that is entirely about the player.

**What done looks like:**

1. **The navigation becomes a column down the left**, inside an ID card that carries the handle,
   the account state and the XP block above it. One object: who you are, where you can go.
2. **The content region takes the rest** and gets a page of its own, so an empty tab reads as a
   page with room on it rather than as a screen with a hole. ⚠️ Narrowing the list also pulls
   `UiRows.ValueColumn` in with it, which shortens the label-to-value journey § 94.7 fault 1
   measured at 1600 px; the value column must stay above 368 units at the narrowest shape, which
   is the number `UiRows.Cap` records and the reason every control in that file is under it.
3. **Every empty state is designed rather than left short**: a fresh career, an empty match
   history, no friends, and a guest account.
4. ⚠️⚠️ **NOTHING IS LOST.** § 119.3's inventory for the hub is CLOSE, six tabs, the footer
   action, the detail view and its BACK, and every `UiRows` row inside all six. The detail popup
   in particular exists because deleting `ProfileOverlay` would otherwise have deleted a shipped
   feature (§ 92.4), and a redesign that quietly loses a screen is a regression wearing a better
   layout.
5. **`PlayerHubLayoutProbe` presses tabs by their lettering**, so the labels may not change, and
   it drives nine resolutions, so nothing here may be a hand-written offset that is correct at one
   of them.

---

### 121.7 What this pass must NOT do

- ⚠️ **Not the main menu and not the in-match HUD.** Scoped out three times now (§ 118.4, § 119,
  and *"except for main menu and actual game for now"*).
- ⚠️ **Not `WoodCraft`.** It draws the main menu and the match. § 121.1 adds a surface to
  `PaperCraft` instead, which is the file that owns the paper front end.
- ⚠️ **Not a fifth hue, and no blue, navy or cold grey in any layer.** § 6.4. The measurement in
  § 121.1 is that rule applied to a shadow nobody had thought to sample.
- ⚠️ **Not a re-baseline of `CarryTests`.** § 93, § 117.8, § 118.4, § 120.9. It is unrelated
  gameplay work and it is red for a reason that is written down three times.
- ⚠️ **Not a chase of `146/146`.** § 120.9 classifies the open PlayMode failures and two of them
  are a documented design conflict rather than a defect; § 121.8 is where that decision gets made
  on evidence rather than on the assertion's say-so.

---

### 121.8 The 16-unit caption question, which § 120.9 left open on purpose

`PaperKit.Caption` is **16** and `MenuKit.MinReadableUnits` is **18**, `PaperKit`'s header states
the conflict as a deliberate decision, and two probes encode the floor as an assertion that cannot
see the argument.

⚠️⚠️ **IT IS SETTLED BY LOOKING AT THE RUNNING BUILD AND NOT BY EITHER FILE WINNING ON PAPER.**
The measurable half is contrast, and it is already good: `PaperInkSoft` on `Paper` is **5.21:1**
and `PaperInk` on `Paper` is **12.34:1** (`sample_png.js contrast`). The unmeasurable half is
whether 16 units is legible at the size he plays at, and the answer differs per screen: a caption
under a `Title` value is a restatement, and a caption that is the ONLY place a fact appears is
not. **Walk every screen that uses one** (sign in, the queue card, the hub, match settings, the
picker, the maker, the lobby drawers) and split them into those two groups before changing a
constant, because raising the constant grows every caption in the front end by an eighth and
`MenuKit.Fit` cannot rescue an overflow below the same floor.

**The walk this entry asks for, done 2026-09-03. `PaperKit.Caption` has 33 call sites across seven
files**, and splitting them the way this entry says to changes the shape of the question.

⚠️⚠️ **THE ANSWER IS NOT 16 OR 18. ONE CONSTANT IS DOING TWO JOBS, AND THAT IS WHY NEITHER NUMBER
IS RIGHT EVERYWHERE.** This is `CLAUDE.md` § 6.5's *"pick a role, not a fill"* one subsystem over:
`WoodCraft.Surface` is a closed list precisely because a screen of twelve plates that were all one
call with a different parameter is what it replaced. `PaperKit.Caption` is that parameter, for type.

**Group A, the caption is a RESTATEMENT** and 16 is defensible: it sits beside or under a value
that already carries the fact, so the small size is what makes it read as a label rather than
competing with the thing it names.

| Where | The caption | The value beside it |
|---|---|---|
| `ConvertedMatchSetup:838` | `CODE` | the join code |
| `LobbyChrome:1495` | `ROOM CODE` | the room code |
| `LobbyChrome:1655` | `YOUR TIER` | the tier |
| `LobbyChrome:1069` | `Skill loadout` | the loadout summary under it |
| `SignInScreen:1273` | the field caption | the field |
| `WoodDropdown:144` | the row caption | the row's value |

**Group B, the caption is the ONLY PLACE THE FACT APPEARS**, and every one of these is a sentence
a player has to be able to read or the screen has failed at something this repository already has
a rule about:

| Where | What it carries | Why it is not a label |
|---|---|---|
| ⚠️⚠️ `SignInScreen:608` `_error` | **why the sign-in failed** | It is the entire explanation. A sign-in that refuses you in type you cannot read is `CLAUDE.md` § 6.3's dead end with a reason printed too small to use. **The strongest case in the list.** |
| ⚠️⚠️ `ConvertedCharacterSelect:1124` | **`Hud.KeyLabelFor(action)`, the live binding** | § 4a: *"prompts read the live binding ... never a literal"*, and `docs/VISION.md` § 3: *"a screen that teaches the wrong key is worse than one that teaches none."* A key printed illegibly teaches none. |
| ⚠️⚠️ `LobbyChrome:1167`, `:1206`, `:1992` | **the one-line summary on a collapsed group's header** | § 6.2 rule 3 states the dependency outright: *"a group closed by default with a one-line summary on its header beats the same rows always open, and the summary is what makes it worth opening."* If the summary cannot be read, the collapse is a hidden feature. |
| `ConvertedCharacterSelect:1280` | an option's description or its unlock challenge | The only place either sentence appears. ⚠️ § 121 already measured this one overflowing at two lines. |
| `QueueCard:423` `_fillCaveat` | the bot-fill caveat | A rule about the match you are about to be put in. |
| `QueueCard:368` `_elapsed` | queue time | The only clock on that card. |
| `ConvertedMatchSetup:3192` `_addressText` | the join address | Nothing else prints it. |
| `LobbyChrome:1679`, `:1845`, `:2596` | note, detail, character loadout | Each is a sole carrier on its own row. |

⚠️⚠️ **SO THE CHEAP CHANGE IS THE WRONG ONE. RAISING `PaperKit.Caption` TO 18 GROWS ALL 33 BY AN
EIGHTH TO FIX 11**, including the six in Group A where the whole point of the size is that the
label does not compete with its value, and § 121.10 row 4 is what that costs: 22 units overflowed
two cells in the picker's tab rail and the fix was to come back DOWN and re-fit. **The Group A
captions are the ones with the least room around them.**

⚠️ **The measurable half is unchanged and still fine**: `PaperInkSoft` on `Paper` is 5.21:1 and
`PaperInk` on `Paper` is 12.34:1.

⚠️⚠️ **STILL OPEN AND STILL 🧑'S CALL, WHICH IS WHAT THIS ENTRY SAYS AND HAS NOT CHANGED.** The
walk narrows it to one question with two answers rather than a constant with two numbers: **does
Group B get its own size (a second `PaperKit` constant at the 18-unit floor, eleven call sites,
Group A untouched), or does 16 hold everywhere because he can read it at the size he plays at?**
The first is the recommendation. ⚠️ **Neither is settled from this file**, and `AspectRatioProbes`
stays red on `DoorCaption` until it is (§ 130.15).

---

### 121.10 ⚠️⚠️ WHAT THE RENDERS CHANGED, AND THREE OF THESE REVERSED A DECISION TAKEN ONE RENDER EARLIER

**This is the section to read before trusting anything above it**, because the plan in § 121.1 to
§ 121.6 was written from crops and four of its calls were wrong in a way only a fresh picture
could show. `CLAUDE.md` § 6.5's closing line, *"take the picture, then take it again"*, is the
whole of this pass.

| # | What the plan said | What the render said | What it is now |
|---|---|---|---|
| 1 | The primary's chamfer is the odd one out, so make it a pill | ⚠️⚠️ **THE CHAMFER WAS NEVER THE FAULT.** `Logs/crops/start-cap-v61.png` at 6x: **two objects stacked** — a new `Action` pill on the node's own Image and 🧑's chamfered `Artwork` child drawing straight over it. That is what he photographed as *"its a circle and a sharp shape at the same time"*, and it is what "rounded shit next to square shit" meant on the maker's footer too. | The child graphics go (`PaperKit.MakeAction`) and the surface is **chamfered again**, on his instruction after seeing both: **"i kinda preferred the sharper edges on this, i js wanted u to make it mroe 3d"**. `CLAUDE.md` § 6.5 is back the right way up: one chamfer per screen, and it is the one action. |
| 2 | Give the primary depth | *"this still looks ugly, especially the shadow"*, *"it feells so flat"* | The face ramp went from a 34-point value spread to **54**, the wall from 16 per cent of the face to **22**, and the cast shadow from a squared falloff over ten units to a **cubed** one: two thirds of the alpha inside the first third of the drop. A blur became a contact shadow. |
| 3 | `Ghost` is the right idle for a tab | ⚠️⚠️ **A TAB ROW WAS TWO SILHOUETTES.** `Logs/crops/picker-tabs-v61.png`: `HERO` a pill, `LATA` and `TSINELAS` 18-unit rounded rectangles, `MAKE YOUR OWN` a pill. `PaperCraft.Surface.Live`'s own note forbids exactly this in writing (*"Same pill ... Giving the selected tab its own shape would say 'these two controls are different KINDS of thing'"*) and nobody had checked it from the idle side. | `PaperKit.MarkLive`'s default idle is `Token`. ⚠️ **AND THERE IS A SECOND DECIDER**: `PaperDress.ButtonSkin` maps `WoodTabIdleButton` to a surface as well, and it runs AFTER `PlayerHub.Highlight` on every tab press, so the hub's column stayed `Ghost` while the picker's row changed. **Both had to say `Token`.** |
| 4 | One size for every cell in the picker's tab rail | 22 units **overflowed** `TSINELAS` and `MAKE YOUR OWN` past their own pills. The three tabs get about 124 units of cell from the `HorizontalLayoutGroup` and the door about 187; `MenuKit.WoodButton` fits its label to the size it is HANDED, which is the 180 and 300 passed in and discarded a frame later. | The size comes down and every cell is re-fitted against the rect the layout group actually gave it, which is the two-step `BuildCustomDoor` already documented and the other three cells never did. |
| 5 | The hub's identity block is a fixed height | On a fresh account it drew a name, a sentence, a rule and then **79 units of nothing**, because the XP guards empty the words and the space was still reserved. 🧑: **"thhis looks really good just tighten it, i dont want huge empty space"** | The block has two heights and `RefreshHeader` picks, and the card is content-height rather than full height. |
| 6 | (not planned) | `LobbyAccount-v61.png`: the account line reads `PLAYING ON THIS MACHINE ONLY · no` and stops. A 44-unit box, a 46-character sentence at 18 units in a 368-unit rail, and `verticalOverflow = Truncate` **drops whole lines in silence**. | Three lines. ⚠️ Truncate is still right; the fix for a truncation is a box that fits the sentence, not a box that lets it escape. |

⚠️ **`tools/sample_png.js` GREW A `crop` MODE FOR THIS AND IT IS WHY ROW 1 WAS FOUND AT ALL.** A
1920x1080 render shown at chat size draws a 500-unit control's keyline about one pixel wide; the
stacked silhouette was invisible in the full frame and unmistakable at 6x. **`CLAUDE.md` § 6.1
says show, do not describe, and a picture too small to read is a description.**

---

### 121.11 What is NOT done, named rather than left implied

⚠️ **EVERY ITEM HERE IS ONE HE RAISED, SO NONE OF THEM IS A NICE-TO-HAVE.** They are open because
they need either a decision only he can make or a reproduction that batch mode cannot produce.

- ⚠️⚠️ **THE CHAT DOES NOT WORK AND IT IS NOT DIAGNOSED.** 🧑: **"chat doesnt work at all btw"**,
  and separately **"also chat is awkwardly placed, it looks very ugly"**, and
  **"can u figure out where to put chat and hwo to make it work bcz it s lowkey ugly"**. Read this
  far and no further, because the reading is not finished: `LobbyChat.Submit` is wired to
  `InputField.onSubmit`, and on a lobby with no peer `MatchRpc.SendChatServerRpc` returns false and
  `AddLocal` is supposed to push *"Not connected. That line was not sent."* onto the log. **His
  screenshot shows an empty log**, so either the submit never fires or the push never draws, and
  those are different bugs in different files. ⚠️ **The host path has its own echo**
  (`HostRelayChat` ends with `OnChatLine?.Invoke`, because `SendNamedMessageToAll` loops back and
  `OnChatLineMsg` refuses the host), so "the sender never sees its own line" is ruled out for a
  host and NOT for a client. **Reproduce it in the player before writing anything**, and a probe
  that types into the field and photographs the result is the cheapest way to make it repeatable.
- ⚠️ **THE LOGIN CARD STILL DOES NOT USE THE SPACE.** § 121.4 has the measurement and the three
  quotes. It is unstarted rather than half-done, deliberately: § 100 is a whole pass spent undoing
  a guess about this exact rectangle, and the two readings of *"use this whole space"* (a wider
  floating card, or a full-height panel bled off the left edge) produce different screens and
  different key-art crops.
- ⚠️ **THE PICKER'S BACKDROP DOES NOT RESPOND TO THE CHARACTER.** § 121.5 has both of his notes and
  they pull against each other: *"this used to be amazing when it was brown only and the
  background corresponded to their color"* and, of the version that tints, *"yea see this doesnt
  look great"*. The resolution written there (vary VALUE inside the paper family and let the hero
  colour in only at low chroma) is a design, not a change.
- ⚠️ **THE ARROWS ARE STILL BLUE**, and § 121.5 has the fresh measurement: about 30 per cent of
  their opaque pixels are hue 201 at 41 per cent saturation, on a field at hue 39. **Both of his
  rules still disagree** and § 120.7 is still right that only he settles it; what is new is a
  third option that does not repaint the file.
- **TAYA FIRST is above the plate and centred and is still the plate's width.** He asked for the
  box to be tightened to its own lettering and said the two boxes need not match; that half is
  done in position and not in size.
- **The 16-unit caption question (§ 121.8) is answered on one control and not as a policy.** The
  lobby's SKILLS row is one size now because he named it (*"these diff fonts look ugly"*); every
  other `PaperKit.Caption` in the front end is untouched and `PlayerHubLayoutProbe` and
  `QueueCardLayoutProbe` still fail on the sign-in screen's and the queue card's.
- **The hub is photographed on a FRESH account only.** § 121.9 asks for a populated one and for
  the long-name state; the long name is shot and the populated career is not, because a probe
  cannot mint a career without writing one.
- **No non-host client pass.** Every shot in this pass is a host that auto-hosted on load.
- ⚠️⚠️ **THE LAN ADDRESS IS STILL TRUNCATED IN THE HOSTING DRAWER, AND FIVE ATTEMPTS DID NOT MOVE
  IT.** `Logs/crops/address-final4.png`: the tray reads `25.3.149.221:8` and the rest is behind
  the COPY button. The row's COLUMNS are fixed (§ 121.10 and `ShareCaption`) and this is the
  remaining half. **What is known:** there is exactly one writer (`ConvertedMatchSetup` line
  ~3000), it demonstrably runs, and changing `fontSize` there produced **no visible change across
  four renders**, with and without a `MenuKit.Fit` after it, with the fit against the rect and
  against a computed width. **So something else is deciding this label's size and it is not that
  line.** Do not tune the number again; find the second writer first. ⚠️ COPY puts the whole
  string on the clipboard, so the feature works and the display does not, which is why this is a
  defect rather than an outage.

---

### 121.9 Acceptance

- Every screen re-shot at `v61` and a person looks at the picture. ⚠️ `UiRuntimeShots.ShotVersion`
  bumps once per iteration, not once per pass.
- **The hub gets shots it has never had**: all six tabs, on a FRESH account and on a populated
  one, plus a long display name.
- `PaperPurityProbe`, `LobbyStyleProbe`, `PlayerHubLayoutProbe`, `UiClickProbe` and
  `AspectRatioProbes` green; Core, EditMode and `Checks.RunAll` green.
- ⚠️ **The five failures § 120.9 names are re-classified against a run rather than inherited**,
  and the splash-shader one gets the confirmation against `9c85c2f` that § 120.9 says it never
  got.
- A clean Windows player on the Desktop, built after every gate above.

---

## 119 · The whole front end is repainted in PAPER, and the lobby is rebuilt around the room ⚠️⚠️ OPEN, 2026-09-01, branch `ui-redesign`

🧑, with a crop of the lobby, a crop of the join panel, `Art/ui/TUMP.png` and a two-swatch card:
*"game reads as too brown bcz the game itself is brown already (the map and shit)"*, *"Look at the
logo, pic 3"*, *"can we remodel the color of all UI for lobby and login to look like this?"*, *"i
want us to play around the 2 colors i attached"*, and then, widening it three times in a row:
*"not just color overaul u can genuinnely overaul the whole thing bcz its ugly"*, *"u can overhaul
the wole lobby and login bcz its ugly as fuck ... it feels overwhelming and not nice to look at as
a user"*, **"redesign teh whole ass UI (dont touch the camera and shit tho) ... ur goal is to make
it inntuitive and easy for user to traverse and calming. I DONT WANT it to be overwhelming for
htem"**.

⚠️⚠️ **HE ALSO GAVE THE PERMISSION § 118.4 SAYS THIS NEEDS, IN ADVANCE AND BY NAME:** *"i think
handoff says u cant recolor and shit but i give u permission to overhaul"*, and
*"OVERHAUL UI FOR EVERYTHING IN LOBBBY INCLUDING AS WELL EVERYTHING U CAN CLICK IN LOBBY LIKE
CHARACTER SELECT, CHARACTER MAKING, SETTINGS, ETC EVERYTHING (except for main menu and actual game
for now)"*. **The main menu and the in-match HUD are still out of scope and so is the lobby
camera.**

### 119.1 ⚠️⚠️ THE DIAGNOSIS IS HIS AND IT IS MEASURABLE: THE UI AND THE WORLD ARE THE SAME COLOUR

*"game reads as too brown bcz the game itself is brown already"*. Sampling
`Logs/shots-runtime/Lobby-v51.png`: Eskinita's road, houses, poles and fences sit at **hue 18 to
40, saturation 30 to 60 per cent**. `UiTheme.WoodFace` `793e1f`, which is every panel, rail, card
and toggle on that screen, is **hue 22 at 74 per cent**. Every surface in the front end is
therefore a slightly darker version of the picture behind it, and the only thing separating the
two is the keyline. **No amount of bevel, grain, varnish or composition fixes that**, which is why
§ 116, § 117 and § 118 each improved the screen and each left him saying it still looked wrong.

⚠️ **`f4ecdd` AND `efdabe` ARE NOT A FIFTH HUE.** Both are hue 34 to 38 at 6 to 20 per cent
saturation, one step off `UiTheme.Cream` `f5e6c8`, which has been in the palette since
`ui_theme.gd`. `CLAUDE.md` § 6.4 is intact: no blue, no navy, no cold grey, and the wood, amber,
green and ink are all unchanged. **What changes is which member of the palette is the FIELD.**
Paper is the surface now; wood is the ink, the frame and his own authored buttons standing on it.

⚠️ **AND THE SWATCHES ARE ALREADY IN HIS OWN MARK.** `Art/ui/TUMP.png` is white lettering with a
sand halo on a linen field; the linen samples `f2ead9`, within a point of `f4ecdd` on every
channel. The palette was on screen in the game logo before he sent the card.

### 119.2 The two new files, and why they are new files rather than an edit

| File | What it is |
|---|---|
| `Runtime/UI/PaperCraft.cs` | Five CONSTRUCTIONS in cut paper: `Sheet`, `Token`, `Tray`, `Ghost`, `Sign`. Each differs in silhouette and relief, not only in fill. Plus `PaperSkin`, the rect watcher, which **destroys any `WoodSkin` on the same node**. |
| `Runtime/UI/PaperKit.cs` | The atoms and the four-step type scale (44 / 26 / 20 / 16), one `Gap` of 12, and `PaperButton`, the pose driver. **Deliberately a kit of parts and not a screen builder**, because a shared `BuildPanel(title, rows)` is exactly how five screens become one screen five times. |

⚠️ **`WoodCraft` IS NOT DELETED AND MUST NOT BE.** It draws every wooden control in the main menu
and in the match, neither of which this pass may touch, and it is the transcription of his
authored art. The two materials share `WoodCraft.Depth` and `WoodCraft.Finish` (made `internal`)
so the two cannot pick up different corner anti-aliasing.

### 119.3 ⚠️⚠️ THE CONTROL INVENTORY. NOTHING ON THIS LIST MAY DISAPPEAR

🧑, twice: *"MAKE SURE EVERYTHING U REPLACED IS ACCOUNTED FOR AND WE DONT LOSE BUTTONS"*, and
*"Im so worried ull leave old UI int he shhit and itll be a mess ... as well as forget UI"*. This
is the answer to the second half, and `PaperPurityProbe` (§ 119.6) is the answer to the first.

**LOBBY** — the converted node names `ConvertedMatchSetup` resolves by name. Renaming or dropping
any of these breaks the wiring silently, which is what the `LobbyChrome` header warns about:

`BackButton` · `StartButton` · `PrimaryButton` · `SeatButton0..3` · `SeatHeading` · `SeatHint` ·
`CharacterButton` · `CharacterSelectPanel` · `MapPreview` · `MapValueLabel` · `ModeValueLabel` ·
`DifficultyValueLabel` · `DetailLabel` · `MapPrevButton` · `MapNextButton` · `ModePrevButton` ·
`ModeNextButton` · `ModeRow` · `DifficultyPrevButton` · `DifficultyNextButton` · `FormatPrevButton`
· `StatusLabel`

**LOBBY** — the controls built in code:

| Control | Lives in | Where it goes now |
|---|---|---|
| BACK | `LobbyChrome.LiftBack` | top rail, far left |
| PRACTICE / MULTIPLAYER tabs | `LobbyChrome.BuildTabs` | top rail, centre |
| YOUR PROFILE door | `LobbyChrome.BuildProfileButton` | **top rail, far right** (🧑: *"i tink te profile screen should be more up instead of being below character select"*) |
| player name field | `LobbyChrome.BuildNameField` | the hub PROFILE tab behind that door |
| CHARACTER door | `LobbyChrome.BuildCharacterButton` | bottom rail, left column, row 1 |
| LOADOUT door | `LobbyChrome.BuildLoadoutButton` | bottom rail, left column, row 2 |
| ROOM CODE + tap to copy | `LobbyChrome.BuildRoomSign` | bottom rail, right column, on the `Sign` surface |
| MATCH SETTINGS toggle + summary | `LobbyChrome.BuildLeftRail` | bottom rail, centre column, above the primary |
| MAP / MODE / BOTS / RULES dropdowns | `ConvertedMatchSetup.BuildSettingsDropdowns`, `WoodDropdown` | the settings drawer, which opens UPWARD out of the rail |
| START MATCH / READY / CONNECTING | `StartButton`, `PrimaryButton` | bottom rail, centre column: the one thing on the screen |
| QUICK MATCH + queue card | `QueueCard` | a drawer above the right column, never a floating corner card |
| JOIN / SERVERS | `LobbyJoinPanel`, `_joinButton`, `_onlineButton` | one chip in the right column, opening the rebuilt takeover |
| CHAT | `LobbyChat` | one chip in the right column, opening a drawer |
| SPECTATE | `_spectate` | the seat plates: a free seat is pressable and says so |
| SECURE YOUR PROGRESS | `LobbyChrome` footer link | a chip only while the account is a guest |
| version stamp | `VersionStamp` | on the top rail, not floating over the road (§ 118.1 row 8) |

**LOGIN** (`SignInScreen`): SIGN IN tab · CREATE tab · username · password · primary
(SIGN IN / CREATE ACCOUNT) · CONTINUE WITH GOOGLE · PLAY AS GUEST · BACK · the WELCOME BACK state
CONTINUE and SIGN IN AS SOMEBODY ELSE · the footer key hints. **Eleven controls across three
states, and § 6.2b first row is why all three get photographed.**

**JOIN A GAME** (`LobbyJoinPanel`): code/IP field · JOIN · six browser rows · BACK TO LOBBY ·
LEAVE GAME.

**CHARACTER SELECT** (`ConvertedCharacterSelect`): `CharPrevButton` · `CharNextButton` ·
`CharValueLabel` · `CharacterPreview` · `TabBar` · `TraitRows` · `NameRow` · `NameCaption` ·
`TaglineLabel` · `ConfirmButton` · `BackButton` · `BackdropGlow`.

**CHARACTER MAKER** (`CustomCharacterScreen`): SLOT 1..3 tabs · SURPRISE ME · PRESETS · BACK ·
KEEP AND USE · the ten wardrobe categories · the colour dial.

**SETTINGS** (`ConvertedSettingsPanel`): `ApplyButton` · `BackButton` · `ResetAllButton` ·
`FullscreenCheck` · `PlayerNameField` · `BindingsList`.

**HUB** (`PlayerHub`): CLOSE · six tabs (PROFILE, FRIENDS, LOADOUT, CAREER, MATCHES, ACCOUNT) ·
the footer action (SAVE / REFRESH) · the detail view BACK · every `UiRows` row inside them.

### 119.4 The lobby new composition: one room, two rails

⚠️⚠️ **THIS IS § 118.1 ROW 2 ANSWERED STRUCTURALLY RATHER THAN BY FILLING THE HOLE.** That row
measures 680 units of empty screen on the left and 475 on the right, between a top band and a
bottom rail, with four corners and nothing between them. The answer is not more furniture in the
middle; it is **two full-width rails and a middle that is only the room**, which is the mechanism
§ 118.3 credits to Rocket League and Overwatch and which Fall Guys uses for exactly this screen.

```
+----------------------------------------------------------------------+
|  < BACK        PRACTICE  .  MULTIPLAYER            [ YOU  > ]   v1.00 |  top rail, 88
+----------------------------------------------------------------------+
|                                                                      |
|      BOT        [ YOU ]        OPEN SEAT        BOT                   |  seat plates
|       o            o               - -            o                   |  the cast
|                                                                      |
+----------------------------------------------------------------------+
|  YOUR FIGHTER >  |  MATCH SETTINGS v |  ROOM   VQ7A     tap to copy   |  bottom rail, 180
|  DANTE . PASIP   |  +--------------+ |  +-----+ +-----+ +-----+       |
|  YOUR BUILD   >  |  |  START MATCH | |  |QUICK| |JOIN | |CHAT |       |
+----------------------------------------------------------------------+
```

- **The middle is never chrome.** Only the cast and the four seat plates.
- **Every drawer opens UPWARD out of the bottom rail and is attached to the column that opened
  it.** Nothing on this screen floats in a corner any more, which is what made the queue card and
  the chat read as unrelated boxes.
- **A free seat is a `Ghost` and says `OPEN SEAT`**, which is § 118.1 row 3 and the Among Us
  mechanism: an empty seat cannot be drawn with a filled surface however it is coloured.
- **The two player-card rows stop being near-twins** (§ 118.1 row 4): FIGHTER is a two-line row
  and BUILD is a one-line row, in a left column that is the only stack of rows on the screen.
- **BACK stops competing with the tabs** (§ 118.1 row 5): it is a small pill at the far edge of
  the rail and the tabs are the only thing in its centre.
- **`tap to copy` sits on the amber band of a `Sign`** (§ 118.1 row 7), the one accent on the
  screen, instead of being 15-unit muted cream on wood.
- **The version stamp sits on the top rail** (§ 118.1 row 8).

### 119.5 Each screen gets its own device, because the repetition complaint is about method

🧑: *"DONT USE THE SAME METHODS IN MAKING DIFF PAGES AND PANELS unless u have to bcz the comment
last time by everyone is that our ui looked bland and repetitive"*.

| Screen | Its device | Why that one |
|---|---|---|
| LOBBY | two full-width rails, empty middle | the room is the picture |
| LOGIN | ONE sticker card over full-bleed key art | it is the logo own construction, and the screen has one job |
| JOIN A GAME | one sheet, one field, and a SEGMENTED list (NEARBY / ONLINE) rather than two always-open sections | pic 2: *"COULD USE ON SOME WORKING ON BCZ IT FEELS OVERWHELMING"*. Two headed sections plus a field plus a footer is four groups; one field and one switchable list is two |
| QUEUE | a drawer that grows out of the rail | it is a state of the lobby, not a separate screen |
| CHARACTER SELECT | a filmstrip: the model large, the roster as a strip under it | it is a picker, and picker content is pictures |
| CHARACTER MAKER | a workbench: the model left, one category open at a time on the right | ten categories cannot all be on screen |
| SETTINGS | a folder: groups closed by default with a one-line summary on each header | `CLAUDE.md` § 6.2 question 3 |
| HUB | an ID card with a tab COLUMN rather than a tab row | six tabs across a header is the row that made § 92 unreadable |

### 119.6 ⚠️⚠️ `PaperPurityProbe` IS THE GATE ON "NO LEFTOVER OLD UI", AND IT IS THE POINT

🧑: *"MAKE SURE U COMPLETELY REPLACE UI BCZ I DOTN WANT LEFTOVER SHIT FROM OLD UI TO STILL BE
FRIGGING WITH US"*.

**Every previous pass in this file was verified by looking at a picture, and a picture cannot see a
surface that is behind another surface or off the edge of a drawer that is currently shut.** The
probe builds the lobby and the login screen, walks every `Image` under them, and fails on any node
that still carries `WoodSkin`, `GodotPanel`, a `GodotTheme.Box` sprite or a `UiMaterials.Plank`
sprite, unless it is:

- one of the authored textures (anything whose sprite comes from `Art/ui/`), or
- inside the match HUD or the main menu, which are out of scope.

⚠️ **IT ALSO ASSERTS THE INVENTORY IN § 119.3**: every named control resolves, is active in at
least one state, and has at least one `onClick` listener. That is the half § 118.5 acceptance
could not cover, and it is *"we dont lose buttons"* written as a test rather than as care.

### 119.7 Acceptance

- Every state photographed over the real background at 1920x1080 **and at his window shape**, with
  `UiRuntimeShots.ShotVersion` bumped every iteration.
- `PaperPurityProbe`, `LobbyStyleProbe`, `QueueCardLayoutProbe`, `PlayerHubLayoutProbe`,
  `UiClickProbe`, `AspectRatioProbes` green, plus Core, EditMode and the full PlayMode suite.
- `Checks.RunAll` five of five and the three `tools/` audits.
- ⚠️ **A person looks at the picture.**

---

### 119.8 ⚠️⚠️ THE REDUNDANCY 🧑 FOUND, AND THE LADDER THAT HAD NO DOOR

🧑, looking at the first paper build: **"dont quick match and start match do the same thing? kinda
confusing no?"**, then the fix in his own words: *"maybe for lobby separate it into ranked and
custom or other shit"*, *"maybe if ull join other server or use lan thats custom"*, *"you know use
other games as referenc"*, and **"make custom and ranked ladder shit diff dont jsut copy paste, bcz
ranked laddder dont need join code"**, *"make it as well na u cant queue with a friend in ranked
ladder or smth"*.

**He is right and the screen had two primaries.** START MATCH loaded an arena with whoever was in
the four seats; QUICK MATCH joined a queue that would find a room and load an arena. Both said "a
match starts now", they sat 400 units apart in one rail, and no position fixes two controls with
the same verb.

⚠️ **THE MECHANISM IS THE ONE EVERY GAME IN § 118.3 USES: one primary verb, and the MODE chosen
beside it.** Rocket League's home screen is PLAY over Casual / Competitive / Private; Overwatch 2
has one button whose LABEL follows the mode selector above it; Valorant puts a mode dropdown next
to one START. None of them ships two buttons that both start a game.

| Mode | The one thing | The primary | The right column | Settings |
|---|---|---|---|---|
| `Practice` | play now, alone | START MATCH | **nothing, and the rail shrinks** | open |
| `Ranked` | climb | FIND A RANKED MATCH | your TIER and the party rule | **locked plate, not a greyed chip** |
| `Custom` | get friends in | START MATCH / READY | the room code plaque, JOIN, CHAT | open |

⚠️⚠️ **AND THE LADDER HAD NEVER BEEN REACHABLE BY ANY PLAYER.** `QueueCard.OnQuickMatchPressed`
passed `QueueStake.Casual` as a literal. `QueueStake.Ranked` exists in the core, `PartyRules.CanQueue`
refuses a full stack and an unsigned member for it, `BotFillRules` has separate timing for it,
`RatingRules` owns five tiers and `MatchStatsCollector` reads
`Matchmaker.Current.Stake == QueueStake.Ranked` to decide whether a result counts. **All of Phase 9
shipped behind a constant no screen could change**, and nothing logged, because casual is a
perfectly valid queue. `QueueCard.Stake` is a field now and the RANKED tab sets it.

⚠️ **THE PARTY RULE IS STATED BEFORE THE PRESS.** `PartyRules.RefusalLabel` writes a good sentence
and the player only ever saw it AFTER pressing, which `CLAUDE.md` § 6.2 calls the INTUITIVE
failure. The tier plate carries *"Solo, or a party of up to three"* and, for a guest, *"The ladder
keeps a rating, so it needs an account. Practice and custom rooms never ask."*

⚠️ **ONE SLOT, THREE OCCUPANTS, EXACTLY ONE VISIBLE.** A separate `RankedButton` was built first and
`Logs/shots-runtime/LobbyRanked-v53.png` killed it: a rounded green rectangle where every other mode
has 🧑's authored chamfered slab. **The one primary has to be one OBJECT** or "always in the same
place" is true of the position and false of everything else. `OnStartPressed` dispatches on the
mode.

### 119.9 ⚠️⚠️ FIVE FAULTS THE RENDERS FOUND AND NO PROBE COULD, AND ONE OF THEM TOOK THREE PASSES

| # | What the picture showed | The cause |
|---|---|---|
| 1 | START MATCH drawn 110 units wide with its label clipped, on PRACTICE, and still wrong after a 1.5 s wait | ⚠️⚠️ **`ArrowButtonView.SetPivot` RE-APPLIES `_offMin` AND `_offMax` EVERY FRAME UNTIL ITS PIVOT LANDS**, and those are the offsets captured when the component last ran: the AUTHORED rect, not the one `LobbyChrome` just gave it. Correct on the main menu, where the pennant keeps its own rect; fatal on any control this pass reparents. `PaperKit.Paperise` switches it off for every node it touches. **Two of 🧑's reports, *"back is brokenn"* and *"te back button still broken"*, were the same bug on a different control**, and the second one is what proved the inset was never the cause. |
| 2 | `SKILLS` drawn through `Standard Build` | Two labels whose boxes overlapped by 46 units. **An overlap between two labels is silent in every direction**; § 102.4 is the same fault measured horizontally. Two boxes that share an edge cannot overlap by construction. |
| 3 | `ROOM CODE` drawn through the code | Same shape, on the plaque: a caption inset 24 from the bottom and a 44-unit value inset 26 from the top overlap by 12 on a 62-unit plate. |
| 4 | The tier plate showed `UNRANKED` and no sentence | The value's rect stretched to the plate's bottom edge and drew over the note. A `Text` draws nothing where it has no glyphs, so the covering label is invisible. |
| 5 | `v1.0.0` drawn through the word BACK | The rail's bottom-left corner and a 44-unit chip's vertical centre are the same 14 units of padding apart. |

⚠️ **AND THE SHOT PASS ITSELF WAS WRONG TWICE.** It opened `LobbyJoinPanel` with `SetActive(true)`
rather than `Open()`, so `Refresh` never ran and `LobbyJoin-v52.png` is four rows reading
`AVAILABLE GAMES APPEAR HERE`; and it photographed the lobby 0.6 s after a tab switch, inside a
0.45 s unfurl with a stagger. **A render of a state the game cannot reach is worse than no render.**

### 119.10 What 🧑 rejected by eye, with the number that agreed with him each time

| His words | The measurement |
|---|---|
| *"this yellow dont look good withh creme too btw"* | `UiTheme.Amber` `ffba00` on `UiTheme.Paper` `f4ecdd` is **1.7:1**. Amber leaves the front end; the marker role moves from HUE to VALUE and the room code is a wood plaque with cream lettering, 10:1. § 118.4's *"amber is the marker"* was written for a WOODEN front end, where amber was the one light thing on a dark screen. **Invert the field and the rule inverts with it.** |
| *"this yellow shit uglyu"*, of `SECURE YOUR PROGRESS` | Same 1.7:1, and 20 characters in a 200-unit chip. It is `ACCOUNT` in ink now. |
| *"maybe bcz u just recolored them all"* | Every control was a flat pill with a halo and a 2-unit lip: nothing had a below, so nothing had a height. Every raised surface casts a shadow inside its own bounds now, and a press collapses it. |
| *"this 2nd pic ugly too its still 2d"* | A `Tray` was one dark band along its top edge, which is a gradient rather than a hole. It is four things now: a hard inner shadow, a wrap down the side walls, a lit floor and a cut edge. |
| *"big ass empty sopace"* | The fighter column was 400 units around a 154-unit name, so the name sat at one edge and its chevron at the other. Sized to content, and every row centres its own strings. |
| *"why is entire right side empty"* | The bottom rail reserved 420 units for a mode column PRACTICE has nothing to put in. The rail has a `ContentSizeFitter` now and the column comes off, so the island re-centres. |
| *"its still so big too"* then *"make taht start match bigger"* | Not contradictory. **The CHROME got tighter and the ACTION got bigger**: `PaperKit.Pad` 18 to 14 and `Gap` 12 to 10, against a primary that went 88 to 104. The ratio that decides whether the button reads as the biggest thing is its height against the 44-unit chip above it: **2.4 to 1, from 1.6 to 1.** |
| *"why does insert player name still live here"* | `PlayerHub.BuildProfileTab` has had a `Display name` row since Phase 1. The rail's field was a second control writing the same string. |

### 119.11 What is NOT done, named rather than left implied ✅ CLOSED BY § 120, 2026-09-02

⚠️ **THE FIRST THREE ARE DONE AND § 120 IS WHERE THEY ARE WRITTEN UP.** 🧑 asked for exactly this
list back: **"PLS FINSH THE STUFF LEFT UNDONE"**. The last two are still open and are still open
for the reasons stated below rather than by oversight; they are repeated in § 120.7 so the next
reader finds them in the newest entry.

- ✅ ⚠️ **The sign-in screen's tab pair still uses `Token` against `Ghost`.** The lobby's uses
  `Live` against `Ghost` after the render showed 4 per cent was not enough; the login screen was
  not re-shot between those two changes and is inconsistent by omission, not by decision.
  **`SignInScreen.SetTab` goes through `PaperKit.MarkLive` now, with four other tab rows that had
  the same fault in three different forms (§ 120.3).**
- ✅ ⚠️ **The login card is 900 units tall around about 700 of content.** The Y offsets inside it
  are the ones `SignInScreen` has always used and they were spaced for a full-height column.
  **Measured: 809 of content, 68 units of margin above and 23 below, and it OVERFLOWED by 43 on
  the Google branch nobody has a client id for. `FitCardToContent` sizes and centres it on its own
  content now (§ 120.6).**
- ✅ **The character select, the character maker and the settings panel are dressed by
  `PaperKit.PaperDress.Screen` and have not been photographed.** The pass converts them; nobody has
  looked at whether the compositions still work in the new material. **Photographed at `v58` by
  `UiRuntimeShots.TheLobbyDoorsDraw` and `TheSettingsPanelDraws`, and all three were broken:
  § 120.4 rows 2, 4 and 5, and § 120.5 rows 1, 2 and 4.**
- **`LobbyChat`'s in-match instance is deliberately untouched** and still wooden, because the
  in-match HUD is out of scope. ⚠️ **Still true and still deliberate.**
- **`UiRuntimeShots` does not photograph the WELCOME BACK state**, which needs an account with a
  password attached and cannot be made in a probe. It is stated in that method rather than skipped.
  ⚠️ **Still true.**

---

## 118 · The lobby is coherent now and it is not finished ⚠️⚠️ OPEN, 2026-09-01, branch `ui-redesign`

🧑, after § 117 landed and he had looked at every render: *"create handoff to improve lobby ui even
furthre bcz it looks kinda ugly in some parts"*, *"ask that thing to critique it as well"*, and
*"tell it to use other games as referenc"*.

⚠️⚠️ **§ 117 FIXED THE LANGUAGE. THIS IS ABOUT THE COMPOSITION, AND THEY ARE DIFFERENT JOBS.**
Every surface on the lobby is now drawn in the geometry his own art is drawn in (`WoodCraft`), the
accent is spent once, and no control is distinguished by hue alone. **None of that is the same as
the screen being well composed**, and the eight rows below are what is left when the material
question is answered. Measured off `Logs/shots-runtime/Lobby-v51.png` and its three sibling states.

### 118.1 The eight things that still read badly, ranked by how much they cost

| # | What | Why it reads badly | The measurement |
|---|---|---|---|
| 1 | ⚠️⚠️ **The chat is a placeholder.** | An empty asphalt well with one muted line at its bottom-left, under a header, with nothing else on that side of the screen. It is the only surface on the lobby that looks unfinished rather than quiet. | The well is about 70 units tall to hold one 18-unit line, and the line sits at its BOTTOM because lines fill upward, so two thirds of it is empty by construction. |
| 2 | ⚠️⚠️ **The screen is four corners and a hole.** | The cast is the picture and the chrome frames it, which is the intended arrangement (§ 116.4), but there is no middle ground at all: nothing lives between the top band and the bottom rails on either side. | Left side: the tab row ends at y≈100 and MATCH SETTINGS starts at y≈780. **680 units of nothing.** Right side: the player card ends at y≈370 and LOBBY & CHAT starts at y≈845. **475 units of nothing.** |
| 3 | **The three seats that are not you say nothing.** | Three identical `BOT` plates and no statement anywhere that three bots will fill in, or that a friend could take one of those seats. The room code is on the card and the empty seats are in the middle, and nothing connects them. | `LobbyNameplates`. A player who has never played this game cannot tell whether BOT means "a bot is here" or "this seat is empty". |
| 4 | **The player card's two wooden rows are still near-twins.** | The character row (62 units, two lines) and the build row (38, one line) share a fill, a chevron and an inset. The footer link and the paper tag both read as their own thing; these two do not. | `LobbyChrome.BuildCharacterButton` and `BuildLoadoutButton`. |
| 5 | **BACK competes with the tab row.** | Same band, same height family, same material, and it is the one control on the screen that leaves. | `LobbyChrome.LiftBack`. |
| 6 | **Nothing moves.** | The main menu's pennants unfurl on every entry (`ArrowButtonView`, and 🧑 asked for that animation by name), and the lobby has no entrance at all: it cuts in fully drawn. | The drawers open and close with no transition either. |
| 7 | **`tap to copy` is 15 units and low contrast** on the one control the screen exists to produce. | `CreamMuted` at 15 on wood, beside a 30-unit amber code. | `LobbyChrome.BuildRoomSign`. |
| 8 | **The version stamp sits on nothing** in the bottom-right corner, over the road. | Every other word on the screen is on a surface. | `VersionStamp`. |

### 118.2 ⚠️⚠️ THE METHOD, AND IT IS NOT "MAKE IT PRETTIER"

**Run `game-ui-design` (installed at `~/.agents/skills/game-ui-design`) as a CRITIC first**, before
writing anything. 🧑 asked for that in as many words. Its `references/patterns.md`,
`sharp_edges.md` and `validations.md` are the three files; `CLAUDE.md` § 6.2, § 6.2b and § 6.2c
and `FUTURE.md` § 0.5b are this repository's own versions of the same questions and they win where
they disagree.

⚠️ **Answer § 6.2's four questions about the lobby out loud before touching it**, and note that
the answer to the first one has changed: START MATCH is unambiguously the one thing now, which it
was not when § 116 was written.

### 118.3 Other games, which 🧑 asked for by name, and what actually transfers

⚠️⚠️ **`FUTURE.md` § 0.5b's warning applies to every row here: COPY THE MECHANISM, NOT THE LOOK.**
The table in that section exists because the screens in § 92 were built by copying screenshots and
were still wrong. **Name what the mechanism assumes about the content, then check whether this
game's content has that shape.**

| Game | The mechanism worth stealing | What it assumes |
|---|---|---|
| **Among Us** | The room code IS the lobby's headline, drawn enormous, and the empty seats are visibly seats. | That the primary job of the lobby is getting three other people INTO it. That is true here and § 118.1 row 3 is the gap. |
| **Fall Guys / Stumble Guys** | The cast stands in a lit room and the chrome hugs the edges; the middle is never chrome. | That there is something worth looking at in the middle. There is: `LobbyCast`. This is what the lobby already does. |
| **Brawl Stars** | One enormous primary in the bottom-left corner the thumb rests in, and everything else is a small chip. | A touch device. The hierarchy transfers; the sizes do not. |
| **Rocket League** | A wide bottom bar that owns every action, so the play area above it is never interrupted. | That the actions fit on one row. Four here (start, quick, join, settings) probably do. |
| **Overwatch 2 / Valorant** | A persistent top rail for identity and a persistent bottom rail for actions, with the middle reserved. | Both have far more chrome than this game does; taking the rails without the content is how § 118.1 row 2 stays true. |
| **Party Animals** | Seats are physical objects in the room, and joining is walking into one. | A 3D lobby with room to move. This game's cast already stands in the street; the seats are `LobbyNameplates`. |

### 118.4 What NOT to do, because it has already been decided

- ⚠️⚠️ **Do not repaint his authored art.** `VISION.md` § 6, `CLAUDE.md` § 6.4 and § 6.5. START
  MATCH is `BUTTON LONG.png` through `ArrowButtonView`, the pennants are his, `TUMP.png` is his.
  The wordmark's CARVE is a tint treatment he asked for by name and the file is untouched.
- ⚠️⚠️ **Do not add a fifth hue, and do not add blue or navy in any layer.** § 6.4, which he has
  now had to state seven times.
- ⚠️ **Do not put the accent back on a drawer toggle or a tab.** § 117.3. Amber is the marker (the
  room code), green is the action (`JOIN BUTTON.png` is authored green), wood is everything else.
- ⚠️ **Do not draw a new surface with `GodotTheme.Box` or `UiMaterials.Plank`.** § 6.5: pick a
  `WoodCraft.Surface` role. Those two are the old language and are kept only for callers that
  cannot know their own height.
- ⚠️ **Do not touch the main menu or the in-match HUD.** Scoped out twice: *"dont touch main menu
  and inngame ui"*.
- ⚠️ **Do not re-baseline `CarryTests`.** § 117.8.

### 118.5 Acceptance

- Every state photographed, over the real background, at 1920x1080 **and at his window shape**
  (`CLAUDE.md` § 6.2b: `Fullscreen` is false in his `settings.json`). `UiRuntimeShots.TheLobbyDraws`
  takes four of the states; **bump `ShotVersion` every iteration** or the review is conducted
  against a cached image.
- `LobbyStyleProbe`, `QueueCardLayoutProbe`, `PlayerHubLayoutProbe`, `UiClickProbe` and
  `AspectRatioProbes` green, plus the full PlayMode suite.
- ⚠️ **A person looks at the picture.** A green layout probe is not a good screen, and § 117.7 is
  seven faults that every probe in this repository was green through.

---

## 96 · OPEN: he has never found the way into the hub ⚠️⚠️

**Reported 2026-08-30, by 🧑, about the shipped screens themselves.** Sent the hub's PROFILE tab
and the sign-in screen: *"i didnnt see that at all bruhh"*, *"didnt see this too"*.

⚠️⚠️ **THE SCREENS ARE FINE AND THAT IS WHY THIS IS WORTH AN ENTRY.** Both are built, both are
reachable, both are measured at nine resolutions by `PlayerHubLayoutProbe`, and both have been
green since § 92. **He has been playing the build and has never opened either of them.**

**The sign-in screen is expected and is not a bug.** § 92.3: *"Signing in never opens by itself."*
It is reached only by pressing something, deliberately, because Phase 1's rule is never to block a
first-time player on a form. A player who has not gone looking for it has not missed anything.

⚠️⚠️ **THE HUB IS THE PROBLEM, AND `PlayerNameplate` IS THE ONLY DOOR.** § 92.4 records that the
plate *"replaces both floating buttons and is the only way in"*, and that was the right call: the
two buttons it replaced were what he complained about (*"look wtf why are these buttons here"*).
**But one small chip in the corner of the title screen is now the sole entrance to four tabs, a
career, a match history and the whole account system**, and the person who commissioned it did not
find it.

⚠️ **`FUTURE.md` § 4.5.3 PREDICTED EXACTLY THIS AND NAMED IT AS THE THING A PROBE CANNOT SEE:**
*"it cannot see a screen that is ugly, and it cannot see a control nobody can find."* The probe
asserts the plate is on screen at all nine resolutions. It is. That is not the same claim as
"somebody looks at it".

**What is NOT known, and must not be guessed at.** Whether the plate is too small, too quiet, in a
corner nobody looks at, or simply does not read as pressable. **Do not fix this by adding a second
door**, which is how the six-button panel happened in the first place. Ask him what he expected to
press, or watch one launch.

**Candidates, cheapest first, none of them chosen:**
1. The plate does not look like a control. It has no press affordance and no hover state.
2. It says the handle and the level and never says what pressing it does.
3. `PLAY / SETTINGS / TUTORIAL / QUIT` is a strong vertical rail and the eye may never leave it.

**Done looks like:** he opens the hub without being told where it is.

---

## 95b · OPEN: nothing asserts that a menu label fits, only that it is legible ⚠️

**Split out of § 95 rather than fixed with it, because the fix and the gate are different jobs.**
`AspectRatioProbes.EveryShippedResolutionKeepsTheWholeAuthoredLayoutOnScreen` checks
`fontSize >= MenuKit.MinReadableUnits` and that rects are inside the canvas. It does **not** check
`preferredWidth <= rect.width`, which is the check that would have caught § 95 the day the
pennants were imported.

⚠️ **IT IS NOT A ONE-LINE ADDITION AND THAT IS WHY IT IS OPEN.** Turning the dump into an
assertion across the whole title screen will surface every other authored label in the converted
`.tscn` set at once, and some of those may be deliberate. **Run the dump first and read the list**,
then decide per label, then assert. `PhaseSurfaceLayoutProbe.DumpOverflowing` is the tool and it
already prints path, string, box, need and font size.

**Done looks like:** `AspectRatioProbes` fails on an overflowing menu label, and every exception
is named in code with a reason rather than being absent from the check.

---

## 72 · Two lobby controls reported dead that every headless check says are alive ⚠️ OPEN

🧑 2026-08-29: *"sa lobby hindi nagana yung player name, hindi makapag input ng name
(singleplayer)"*, *"hindi maka input ng code and lobby code sa lobby"*, and, confirming the
first: *"apparently u cant set ur name in singleplayer too"*.

**Both reproduce for the player and neither reproduces headlessly.** Written down rather than
guessed at, because the obvious fix was tried first and came back green.

What has been ruled out, and how:

- **Something covering the control.** `UiClickProbe` was widened to enumerate `InputField`,
  which its own note asked to be done deliberately rather than by accident, and the lobby's
  join card was added to its overlay list (it is built from code and parked inactive, so
  nothing in the suite had ever opened it). `PlayerNameEdit` and `JoinAddressEdit` both report
  `ok`: the topmost raycast hit at each field's centre is the field itself.
- **The click not taking the caret, or losing it.** `LobbyTypingProbe` is new and walks the next
  two steps of the same press: pointer-down and click through the EventSystem, then the
  selection re-read ten frames later. Both fields take the selection and keep it.
- **Legacy input being switched off.** `activeInputHandler` is 2, which is Both, and the
  `MatchSetup` scene's EventSystem carries a `StandaloneInputModule`.
- **The lobby chat stealing focus.** `LobbyChat` calls `ActivateInputField` from three places,
  but the only one reachable without typing in the chat first is gated on Return, and its
  `Update` returns early in the lobby before reaching it.

⚠️ The probes run a lobby with `SceneFlow.Networked` false, which IS the singleplayer case both
name-field reports name, so that is not the gap either.

**What has not been ruled out:** ✅ **the live NETWORKED lobby is ruled out as of § 77.4.**
`NetworkedLobbyTypingProbe` starts a real host through `NetSession.StartHostAsync`, sets
`SceneFlow.Networked`, and runs `LobbyTypingProbe.Check` itself rather than a copy of it, so the
two probes differ in exactly one thing. It passes, and it logs an inventory naming every
`InputField` and whether `LobbyChat` was live while it passed, because a pass with no chat in the
scene would rule out nothing.

⚠⚠ **SO ONE SUSPECT IS LEFT AND IT IS THE BUILT PLAYER AS OPPOSED TO THE EDITOR**, which is
the only item on this list no probe in this repository can reach. Everything else named here has
now been driven. `LobbyChat`'s `OnPointerClick` exists because a press
that missed its field was being swallowed by a plate, which is evidence this class of failure is
real on this screen even though neither probe can currently produce it.

**Done looks like:** a probe that reproduces it, then a fix. ⚠️ Do not "fix" this blind by
adding an `ActivateInputField` to both fields. That is the workaround `LobbyChat` already
carries, it would make the report go away without anybody knowing what was wrong, and the same
cause would surface on the next field somebody adds.

---

## 68 · The lobby is a form, and it should be a room ⚠️ OPEN, PLANNED 2026-08-28

🧑, 2026-08-28, with a PUBG lobby screenshot beside a capture of ours: *"i want our boring ass
lobby to look like this"*, *"i want multiplayer to go straight to lobby and thats where u can
join"*, *"all the shit like map mode bots character join yk everything is togglable"*, *"its okay
if character select still makes u go to a dif screen. i dont want character select to be
touched"*, *"make sure u dont break lobby"*, *"dont delete old huds and ui tho keep them incase ur
shit turns ugly"*, *"organize everything and make sure theres no redudnant shit"*, *"make sure
shit works like end to end"*.

**The reference is the LAYOUT, never the skin.** PUBG's lobby is grey military chrome and this
game's brand is painted wood, cream and amber (`UiTheme`, and `Art_Direction.md` § 1 says the
palette file is the only place a colour is named). What is borrowed is the ARRANGEMENT: the room
is the picture, the cast stands in it, the controls are small furniture pushed to the edges.

### 68.0 The four decisions, taken 2026-08-28 before any code

Asked and answered rather than assumed, because each one changes what gets built:

| Question | Answer |
|---|---|
| Network state on arriving at the lobby | **Auto-host on LAN.** § 68.5 |
| Which of the two references | **Hybrid**: PUBG Mobile's chrome, PC PUBG's four-person line-up. § 68.7 |
| Chat | **Lobby AND in-match, on this branch.** § 69, and it is the only wire change |
| Nav bar | **`PRACTICE ǀ MULTIPLAYER` tabs.** § 68.7 |

### 68.1 What the two screens actually are today

`ConvertedMatchSetup` is ALREADY both screens. It draws `PRACTICE MODE` offline and `LOBBY`
when `NetSession.IsNetworked`, off one `isNetworked` branch in `Refresh()`. Nothing has to be
invented to make multiplayer land here; the screen has been the lobby since § 55.

What lives on the OTHER screen, `ConvertedMultiplayerSetup`, is only the four ways IN: host
online (Relay), host LAN, a join address/code field, and the two browsers (`LanBeacon` and
`ServerQuery`). Those are the only things that have to move.

⚠️ **THE FOUR SELECTORS ALREADY REPLICATE AND THE READY TALLY ALREADY ARRIVES.** Map
(`SelectMapServerRpc`), mode (`SelectModeServerRpc`), difficulty (`SelectDifficultyServerRpc`),
picks (`SelectLobbyPickServerRpc`), the seat table (`LobbySeatInfo`, which carries `Name`,
`Occupied` and `CharacterPick`) and the ready tally (`OnLobbyReadyChanged`) are all on the wire
already. **The lobby redesign is therefore a pure client-side reskin.**

### 68.2 ⚠️⚠️ THE LOBBY WORK ADDS NO WIRE CHANGE. CHAT IS THE ONLY ONE, AND IT BUMPS THE PROTOCOL

Everything the new lobby draws is already replicated (§ 68.1), so § 68's own work moves
`NetSession.ProtocolVersion` not at all. **§ 69's chat does**, 5 → 6, because a chat line is a
named message that has never existed.

⚠️ **THAT MEANS BOTH LAPTOPS MUST BE REBUILT FROM THIS BRANCH.** § 59.4 records what a bump
costs: a peer on a different protocol is REFUSED at approval, by design, because a build that
"mostly works" presents as wrong characters and frozen bodies. § 59.2 is what makes the refusal
say so out loud instead of hanging. **Land and verify § 68 with the protocol still at 5, bump it
once in § 69, and never twice.**

⚠️ If a step in § 68 seems to need a new field, that step is wrong.
`tools/audit_wire_payloads.py` is the check, and `audit_request_call_sites.py` catches the other
half (a protocol added and never called).

### 68.3 ⚠️⚠️ THE OLD CHROME IS KEPT AND SWITCHABLE, NOT DELETED

🧑: *"dont delete old huds and ui tho keep them incase ur shit turns ugly"*.

`LobbyStyle` is one enum with two values, read once in `ConvertedMatchSetup.Wire()`:

* `Classic`, the authored converted panels exactly as they are today. Nothing new is drawn.
* `Street`, the new arrangement. Default.

⚠️ **THE OLD NODES ARE DEACTIVATED, NEVER DESTROYED**, and the new chrome is BUILT FROM CODE in
`Wire()` the way `BuildRightPanelNetwork` already builds the address and code rows. So
`MatchSetup.unity` barely changes on disk, `Classic` is a working screen at every commit, and
reverting is a one-line default rather than git archaeology.

⚠️ **`MultiplayerSetup.unity` AND `ConvertedMultiplayerSetup.cs` STAY ON DISK AND STAY IN THE
BUILD ORDER.** They cost one scene entry and they are the fallback if the in-lobby join panel
turns out worse. Only the LINK from `ConvertedModeSelect` is removed. `UiClickProbe`,
`ScreenshotTool` and `UiRuntimeShots` keep passing because the scene still exists.

### 68.4 ⚠️⚠️ RE-SKIN, DO NOT RENAME

`ConvertedScreen` finds every control by the name Godot gave it and `Node()` logs an error on a
miss. `SeatButton0..3`, `PrimaryButton`, `StartButton`, `BackButton`, `MapValueLabel`,
`ModeValueLabel`, `DifficultyValueLabel`, `CharacterButton`, `BannerLabel`, `DetailLabel`,
`SeatHeading`, `SeatHint`, `StatusLabel`, `MapPreview` and `CharacterSelectPanel` keep their
names and their handlers. **Renaming one is how this breaks silently**, and it is the exact
failure that class header exists to describe.

### 68.5 The navigation change, and the third lobby state it creates

`ConvertedModeSelect` sends MULTIPLAYER to `SceneFlow.MultiplayerSetup`. It goes to
`SceneFlow.MatchSetup` with `Networked = true`, **and the lobby auto-hosts on LAN the moment it
arrives.**

That gives the screen a state it has never had: networked, but no transport yet. Today
`IsNetworked` is false until somebody hosts or joins and the screen reads that as practice.

| State | Headline | Selector rows | Join panel |
|---|---|---|---|
| Practice (offline) | `PRACTICE MODE` | all live | hidden |
| Lobby, host bind failed | `LOBBY` + the reason | live, local only | OPEN |
| Lobby, hosting | `LOBBY · YOU ARE HOSTING` | live, replicated | available |
| Lobby, connected | `LOBBY · CONNECTED` | greyed, host picks | LEAVE |

⚠️⚠️ **A REFUSED PORT BIND MUST FALL BACK, NEVER HARD-FAIL.** Auto-hosting binds 8910 the moment
somebody presses MULTIPLAYER, and the usual reason it is already bound is the player's own second
copy of the game. The screen drops to row 2 with `NetSession.Status` on the status label and the
join panel already open, so the path OUT is on screen rather than being an error message. That is
`ConvertedMultiplayerSetup.Reason()`'s finding applied one screen earlier.

### 68.6 ⚠️⚠️ HOST → LEAVE → JOIN IN ONE LAUNCH IS THE PATH THIS FEATURE LIVES OR DIES ON

Auto-hosting means joining somebody else is STOPPING a host and STARTING a client in the same
process. That is § 65.1's fault (`NetworkManager.Shutdown()` does not shut anything down; a
second host or join in one launch was refused, silently) and § 63.1's (handlers registered once
per process, not once per session). **Both are fixed. Neither has ever been exercised in this
order.** § 68.14's two-process run exists for this and nothing else.

### 68.7 The arrangement: PUBG Mobile's chrome, PC PUBG's line-up

Hybrid, as decided. The bottom nav from the mobile shot is DROPPED rather than invented: its
tabs are RANK / SEASON / WORKSHOP / MISSIONS / INVENTORY and this game has none of them. The two
tabs that are real go along the top.

| Reference | Here | Built from |
|---|---|---|
| Full-bleed 3D scene | The chosen arena, live, still swaying | `MapPreviewSurface`, already shipped |
| Four players standing in it (pic 1) | The four seats as their picked characters | NEW, § 68.8 |
| Names + ready ticks over their heads | Same, plus a `TAYA FIRST` tag | NEW, § 68.9 |
| Top nav (pic 1) | `PRACTICE ǀ MULTIPLAYER` tabs | `MenuKit`, wood |
| Player card top-left (pic 3) | Name, avatar, the character you picked | `MenuKit` |
| Stacked selectors bottom-left | MAP / MODE / BOTS / CHARACTER | The existing selectors, restyled |
| Big yellow START bottom-left (pic 3) | `PrimaryButton` / `StartButton`, amber | Existing nodes, restyled |
| Bottom-right LEAVE + ticks + cog | LEAVE / SPECTATE / settings | Existing `BackButton`, `SpectateButton` |
| Party code / region | Join code + address + JOIN | Lifted, § 68.11 |
| (none) | Chat, bottom-left above START | § 69 |

⚠️⚠️ **THE TABS SWITCH IN PLACE, WITH NO SCENE CHANGE.** `PRACTICE` stops the transport and
clears `SceneFlow.Networked`; `MULTIPLAYER` sets it and auto-hosts. Both then re-run the same
`Refresh()`. A scene reload here would tear down the cast, the cached arenas and both render
textures the screen just built, and `SceneFlow.Go`'s one-load-per-frame latch will not save a
same-scene reload because it is scoped to a single frame on purpose.

⚠️ **THE PANELS GET SMALLER, THEY DO NOT GET TRANSPARENT.** `UiTheme.HeroPlate`'s note is
explicit that translucent near-black is COMBAT chrome, where the court behind it is the subject,
and that menu chrome is FURNITURE and may be opaque. The room becomes the picture by pushing
opaque wooden furniture to the EDGES, which is what both references actually do. A translucent
wood panel is the "brown shit" 🧑 already rejected once.

⚠️ **THE SCRIM CHANGES SHAPE, NOT STRENGTH.** `Scrim` currently flattens the whole backdrop. It
becomes a top and bottom gradient so the middle of the room is clean and the text still reads.

### 68.8 The cast, which is the whole feature and the only real engineering

Four characters standing on the map, lit by the map's own sun and graded by the map's own
`MapGrade`.

⚠️⚠️ **THEY GO INSIDE THE PREVIEW ARENA, NOT INTO A SECOND RENDER TEXTURE.** `ModelPreview` draws
ONE subject on layer 30 with its own lights and its own camera; four of those composited over the
map would be four cameras, four targets and four subjects lit by nothing the map knows about,
which is the pasted-on look. `MapPreviewSurface` already loads the arena, strips the match out of
it, confines it to layer 29, copies the arena's ambient, fog, sky and colour grade, **and already
finds the map's `SpawnPoints` because it averages them for its camera pivot.** The cast stands on
those same markers.

Shape of the work:

* `MapPreviewSurface` grows `ShowCast(...)`. Models are parented into the cached arena scene
  **AFTER `StripMatchObjects`**, because that method destroys every `CharacterMotor` GameObject it can
  reach, and although a plain roster prefab has no motor, ordering it wrongly is a silent
  deletion of the cast. They are then re-layered to 29 with the existing `SetLayerRecursively`.
* Art comes from `RosterBook.PersonArt(index, mode)` → `.Model`, `.Clips`, `.Palette`, exactly as
  `ConvertedCharacterSelect` resolves it. ⚠️ Rigs are imported **Generic**, so
  `ModelPreview.EnsureAvatar` is required or every model stands in its bind pose, arms out. That
  is `ModelPreview`'s own recorded fault 4, and it also wrecks any framing measured off the
  silhouette.
* A seat's character is `LobbySeatInfo.CharacterPick` when occupied and the roster default when
  it is a bot. ⚠️ `RosterBook`'s header: a missing entry must render SOMEBODY, never nothing.
* Scale is `ModelPreview.PreviewScale` 2.38, which is the match's own PERSON_SCALE. Previewing at
  native scale frames a doll.
* Framing: the registry's per-map `Yaw`/`Distance`/`Height` were tuned for an EMPTY street
  (Eskinita 0/22/16). A four-person line-up needs its own shot, so one `LobbyFraming` joins the
  three existing fields on `MapEntry`. ⚠️ It goes in the registry and not in the map scene, for
  the reason that struct's own note gives: `tools/maps/build_*.py` emit the map scenes
  WHOLESALE, so a camera placed by hand survives exactly until the next layout run.
* ⚠️ The sway stays. `SwayDegrees` 7 over `SwayPeriod` 26 is what stops the shot being a
  photograph, and it is a sway rather than an orbit so the camera never swings behind the facades.

### 68.9 Nameplates

A name, a ready tick and a `TAYA FIRST` tag floating over each character. UI, not world geometry:
projected with the preview camera's `WorldToViewportPoint` and mapped into the `MapPreview`
RawImage rect.

⚠️ **THEY ARE NOT TINTED WITH `Offense` OR `Defense`.** Those two colours mean "attacker" and
"defender", and `UiTheme.ForRole`'s note is explicit that the taya ROTATES every round, so a
fixed per-seat colour tells the player the wrong thing three rounds out of four. Cream for names,
Amber for the taya tag, nothing else.

⚠️ **EVERY PLATE IS SIZED AGAINST ITS STRING.** Legacy `Text` defaults to WRAP and the converted
labels ship `Overflow`, so a long player name either wraps out of its plate or draws straight past
it. `ConvertedScreen.SetHeadline` records this happening three times in one session and
`GameVersion.ApplyTo` records the fourth. A player name is arbitrary text from another machine,
which makes this the worst case in the game, not the mildest.

⚠️ **`raycastTarget = false` ON EVERY DECORATIVE GRAPHIC**, or `UiClickProbe` reports the controls
underneath as unreachable and it will be right.

### 68.10 Everything togglable, and the greying that is missing today

MAP, MODE, BOTS, CHARACTER, seat, SPECTATE, READY, START, the `PRACTICE ǀ MULTIPLAYER` tabs, and
the LAN/ONLINE choice in the join panel.

⚠️ **A NON-HOST'S CYCLE BUTTONS SILENTLY DO NOTHING TODAY.** `OnMapCycle`, `OnModeCycle` and
`OnDifficultyCycle` all open with `if (!NetAuthority.IsHost && SceneFlow.Networked) return;`,
which is correct authority and a bad control: the button lights, clicks, plays its sound and
changes nothing, which is indistinguishable from broken. They get `interactable = false` and a
line saying the leader picks. **This is a live defect being fixed in passing, not a new feature.**

⚠️ **BOTS STOPS AT HARD IN A NETWORKED LOBBY**, and that is `DifficultyOptionCount`'s existing
rule rather than an oversight: `NONE` removes three seats, and a seat is what a peer joins.

⚠️⚠️ **THE BIG AMBER BUTTON IS `START MATCH` FOR THE HOST AND `READY` FOR EVERYONE ELSE.** 🧑,
2026-08-28: *"start should be ready for everyone else except for host"*. One button in one place,
two labels, decided by `NetAuthority.IsHost`. **That is a layout change, not a behaviour change:**
§ 59.3 already made readiness an ANSWER the host reads rather than a trigger, on request (*"i
also dont like that if u click ready it auto starts"*), and the host's START is already live
whatever the tally reads, because a host plus three bots is a legitimate match.

⚠️ **SO `StartButton` AND `PrimaryButton` STOP BEING TWO CONTROLS ON SCREEN AT ONCE.** Today the
host sees both: `PrimaryButton` reads READY and `StartButton` is shown host-only right beside it.
In `Street` exactly one of them is visible per peer. ⚠️ **Both nodes stay in the scene and keep
their handlers** (§ 68.4); the one that is not this peer's is deactivated, not rewired, so
`OnPrimaryPressed` and `OnStartPressed` keep the meanings § 54 and § 59.3 settled.

⚠️ **CHARACTER STILL OPENS THE EXISTING PANEL IN PLACE.** 🧑: *"i dont want character select to be
touched"*. `ConvertedCharacterSelect.cs` and `CharacterSelect.unity` are not edited, and
`OpenCharacterSelect` keeps revealing `CharacterSelectPanel` as a child of this scene. § 68.13.

### 68.11 The join panel, lifted rather than rewritten

`ConvertedMultiplayerSetup`'s LAN browser, online browser, address/code field and Relay host
button move into a `LobbyJoinPanel` opened from the lobby. The logic is transcribed, not
redesigned: `Reason()` (which stopped four different failures reading as one sentence),
`LastDisconnectReason` (read once and cleared), the `host:port` split from § 59.1, and the code
lookup through `ServerQuery.ResolveCodeAsync`.

⚠️⚠️ **ONLINE IS A FIRST-CLASS LOBBY, NOT A LEFTOVER OF THE OLD SCREEN.** 🧑: *"make sure u can do
online server lobby too"*. Auto-hosting on LAN (§ 68.5) is the LANDING state and not the only
one. The lobby carries a `LAN ǀ ONLINE` toggle beside the join code:

* **LAN → ONLINE** stops the local host and calls `NetSession.StartRelayHost()`, then redraws in
  place. The join code row swaps from `address + code` to the Relay code, and `ServerQuery`
  publishes the lobby to the online pool so it shows up in other players' browsers.
* **ONLINE → LAN** is the same move back.
* **Joining** is symmetric and already is: `ResolveCodeAsync` returns `IsLan`, and the panel takes
  `StartClientAsync` or `StartRelayClient` off that flag. A four-character code works for both, so
  a player reading a code out loud never has to know which kind of lobby they are in.

⚠️ **A TOGGLE IS A SECOND HOST → LEAVE → HOST IN ONE LAUNCH**, which is § 65.1 again, from a
third direction. It is on the two-process list (§ 68.14 step 7).

⚠️ **AND ONLINE HAS ONE OPEN FAULT ALREADY: § 65.4**, the online browser can offer a lobby whose
Relay allocation is gone. Moving the browser does not fix it and must not hide it; the failure
has to reach the status label through `Reason()` like every other one.

⚠️⚠️ **`SceneFlow.Go(MatchSetup)` AFTER A SUCCESSFUL JOIN BECOMES A REFRESH IN PLACE.** The player
is already on that scene. Reloading would destroy the cast, the cached arenas and the render
textures, and the one-load-per-frame latch does not cover it.

⚠️⚠️ **AND THE REJOIN PATH MUST STILL FIRE.** `RejoinRunningMatch` runs inside `Wire()` and reads
`Lobby.MatchInProgress` on arrival; joining in place never re-runs `Wire()`. Its header records
what that hole costs: *"you'll only get ported back to the lobby with no way of joining back"*.
Whatever replaces the navigation has to ask the same question again at the same moment.

### 68.12 Organisation, so nothing lives in two places

🧑: *"organize everything and make sure theres no redudnant shit and that everything is easy to
find"*.

| File | Owns | Knows about |
|---|---|---|
| `ConvertedMatchSetup.cs` | The state machine and the wiring | Everything below |
| `LobbyChrome.cs` (new) | Building the `Street` furniture and the tabs | `MenuKit`, `UiTheme` |
| `LobbyCast.cs` (new) | The four models and their nameplates | `RosterBook`, `MapPreviewSurface` |
| `LobbyJoinPanel.cs` (new) | Hosting, joining, both browsers | `NetSession` |
| `LobbyChat.cs` (new, § 69) | The chat log and entry field | `MatchRpc` |
| `MapPreviewSurface.cs` | The room. Grows `ShowCast` + lobby framing | The arena scenes |
| `ConvertedMultiplayerSetup.cs` | Nothing. Unreferenced, kept as the fallback | (nothing) |

⚠️ **ONE COLOUR SOURCE AND ONE CONTROL BUILDER.** Every colour from `UiTheme`, every control
through `MenuKit`/`GodotTheme` so `GodotButton` variations keep applying. `UiTheme`'s header
records the whole hero layer drifting into a slate-blue palette because seventeen colours were
named inline; this is the same trap on a different screen.

### 68.13 What must NOT be touched

* `ConvertedCharacterSelect.cs`, `CharacterSelect.unity`, by request. `ModelPreviewTests` and
  `HeroPickerLayoutProbe` passing unchanged is the proof.
* `Packages/com.tumbangpreso.core/`, engine-free, and no rule changes here.
* `MatchRpc` payloads, until § 69 and then exactly once.
* `ConvertedMultiplayerSetup.cs`, `MultiplayerSetup.unity`, kept per § 68.3.
* `GameVersion` / `BuildBranch`. The corner reads `1.00` on every branch as of 2026-08-28.

### 68.14 Done looks like

1. `dotnet test Core.Tests` green.
2. EditMode green, `LobbyAndSettingsTests` included, plus a new test asserting that BOTH
   `Classic` and `Street` resolve every node `ConvertedMatchSetup` reaches by name. That test is
   what makes § 68.4 an assertion instead of a warning.
3. PlayMode green (no `-nographics`, it crashes the editor; assert on the `.xml`, never the exit
   code, because both a crash and a failure come back as 0): `UiClickProbe` finds every new
   control reachable, `AspectRatioProbes` clears nine resolutions, `HeroPickerLayoutProbe` and
   `ModelPreviewTests` still pass.
4. `Checks.RunAll` green, `SceneScriptCheck` above all: it is the only check that can see a scene
   holding a component the PLAYER cannot bind, and a shipped build once crashed on the map select
   with every other check green.
5. `tools/audit_wire_payloads.py` and `audit_request_call_sites.py` exit zero: § 68 adds no
   protocol, § 69 adds exactly one and it is called.
6. `UiRuntimeShots` captures of the lobby in BOTH styles, versioned filenames per `CLAUDE.md`
   § 6.1, so `Classic` and `Street` can be compared side by side rather than described.
7. ⚠️⚠️ **THE TWO-PROCESS RUN, AND IT IS THE ACCEPTANCE TEST.** § 38.19's driver, in this exact
   order, because the order is what is untested:
   1. A presses MULTIPLAYER, lands hosting, shows a join code.
   2. B presses MULTIPLAYER, lands hosting its OWN lobby.
   3. B opens JOIN, sees A on the LAN browser, joins. **This is the host → leave → join of
      § 68.6.**
   4. A's cast grows a second body wearing B's character, with B's name over it.
   5. B changes character; A sees the model change. B readies; A's tally moves.
   6. A starts; both land in the same arena on the same map in the same mode.
   7. B quits to the lobby and joins A AGAIN in the same launch (§ 65.1).
   8. B rejoins while the match is still running (§ 68.11's `RejoinRunningMatch`).
   9. Chat carries in both directions in the lobby and in the arena (§ 69).
8. Clean Windows build, previous output deleted first, timestamps verified on BOTH
   `TumbangPreso.exe` and `TumbangPreso_Data`. A `SUCCEEDED` line does not prove the launcher
   was re-emitted.

### 68.15 Order of work, so the screen is never half-broken

1. Navigation, auto-host, and the four lobby states. **No visual change at all.** Verify joining
   works from the lobby before anything is made pretty.
2. `LobbyJoinPanel`, lifted.
3. The cast in the backdrop.
4. The `Street` chrome and the tabs, behind the `LobbyStyle` switch.
5. Nameplates.
6. § 69's chat, and the single protocol bump.
7. The full verification pass in § 68.14.

### 68.17 What landed on the `PUBG` branch, and the five things the renders found

Steps 1 to 5 of § 68.15 are in. Every number below was MEASURED off a capture rather than
argued, which is the only reason any of them is right: five of the six were wrong on the first
pass and none of the six would have been caught by a test.

**The files.** `LobbyChrome.cs` (the `Street` arrangement and the tabs), `LobbyCast.cs` (the four
bodies), `LobbyNameplates.cs` (the plates over them), `LobbyJoinPanel.cs` (host, join, both
browsers), `LobbyChat.cs` (§ 69, used by the lobby AND the arena). `ConvertedMatchSetup` gained
the state machine and nothing else; `MapPreviewSurface` gained `Adopt`, `MapShown` and the lobby
shot; `MenuKit` gained `Fit`, `FitBox` and `FitBlock`.

**⚠️⚠️ THE CAST FACED THE WRONG WAY, AND THE NOTE THAT SAYS SO IS MISLEADING.**
`ModelPreview.FacingYaw` is 180 with a header about Godot's handedness, and reading it as "the
model's front is its local -Z" is the wrong inference. `Lobby-v1.png` is four backs.
`LookRotation` aligns local **+Z**, and these rigs face **+Z**, so the direction to point along is
`-forward` (subject toward camera). One sign, and no test in this repo can see it.

**⚠️⚠️ A RECT HANDED TO A LAYOUT GROUP IS A REQUEST, NOT AN INSTRUCTION, AND THAT COST THREE
RENDERS.** `LeftColumn` was set to 580 and `ReportColumns` measured it at 580, and the panel
inside it drew **820**. Three separate things get to overrule the width: the authored
`VerticalLayoutGroup` ships with `childControlWidth` OFF so it positions children without sizing
them, a child's `LayoutElement.minWidth` outranks the group even once control is on, and a child's
own `ContentSizeFitter` rewrites the rect after the group has finished. `Narrow` answers all
three. **`localScale` is what actually settled it**, because nothing in Unity's layout reads it,
and it shrinks the panel WITH its type and its borders, which a width alone does not.
`LeftScale` 0.72 and `RightScale` 0.86 open the middle band from 320 px to about 800.

**⚠️⚠️ THE FIT PASS HAS TO RUN MORE THAN ONCE.** `Lobby-v2.png` still reads `LOBBY · YOU ARE
HOSTIN` under the SPECTATE button after a fit pass that had already run and reported success: the
widths it measured came from a chain of layout groups that had not converged, so it measured
against a width nothing would ever have and concluded the string fitted. It now repeats for
`FitPasses` frames and forces a real `LayoutRebuilder` pass first, because
`Canvas.ForceUpdateCanvases` flushes the canvas and does not run the layout system.

**⚠️ THE NAMEPLATES WERE ALL IN THE BOTTOM-LEFT CORNER**, drawn as four stray `BOT` chips over the
BACK button, which reads as a chrome bug rather than a projection one. A plate is anchored at
(0,0), so its `anchoredPosition` is already measured from the parent's bottom-left; adding
`rect.xMin`, which on a centred-pivot stretched rect is minus half the width, subtracted half a
screen twice.

**⚠️ THE LOBBY SHOT NEEDED ITS OWN LENS, NOT JUST ITS OWN DISTANCE.** Framing four people to fill
half the height at the map shot's 58 degrees puts the camera about 3 m away and leaves the outer
two at 34 degrees off axis, visibly stretched. `LobbyFieldOfView` 32 puts the same framing about
7 m back at 17 degrees off axis, and keeps more of the street readable behind them.
`LobbyDistance` 12.6 and `LobbyHeight` 3.4 with `LobbyLookHeight` 0.85: aiming LOWER is what
lifts the cast clear of the corner furniture without changing how big they are.

**⚠️ THE ACTIVE TAB IS AMBER, NOT GREEN.** `WoodPrimaryButton` is green and means ACT (START
MATCH, READY); a tab is not an action, it is a statement about where you already are, and painting
it green put two "press me" buttons on one screen with the more important one further from the
hand. `WoodAmberButton` is new in `GodotTheme` and introduces no colour: amber is already this
UI's attention colour and is in `UiTheme`.

**What is verified.** `dotnet test` 111 green. `Checks.RunAll` all five green.
`audit_wire_payloads.py` 47 named messages, 0 mismatched, `Chat` and `ChatLine` among them.
`audit_request_call_sites.py` 43 entry points, 0 unreachable. `audit_ability_authority.py` 40
sites, 0 ungated on another body. `TheLobbyDraws` passes and writes
`Logs/shots-runtime/Lobby-v*.png`.

**⚠️ WHAT IS NOT VERIFIED IS THE ONLY THING THAT MATTERS: § 68.14 STEP 7, THE TWO-PROCESS RUN.**
Every fault this batch could still hold is on the far side of a second machine: host to leave to
join in one launch (§ 68.6), the LAN/online toggle, a joiner's cast wearing the right character,
the ready ticks moving on somebody else's screen, and chat in both directions. **Nothing here has
been played by two people.**

---

### 68.18 The second pass, 2026-08-28: navigation, the settings panel, and one rail per side

🧑, off the 4.7x player: *"Rewire clicking play from main menu to directly the lobby bcz we dont
need single player multiplayer selection anymroe as practice is bascally singleplayer already"*,
*"match settings look ugly"*, *"also maybe plan out where to put ui for char select, remove it in
match settings"*, *"pic 3 doesnt have animations or move but everyone else does in here"*, *"Also
rewire tutorial from main menu to the start training already, the text based tutorial is stale and
should be deleted and completley replaced by game tutorial"*, *"Pic 4 fix player name"*, *"Also
sometimes the pillars in the ilalim ng tulay map block the camera of lobby"*, *"put BACK somewhere
else, it looks ugly that its right below start match, it fucks up the visual hierarchy"*, *"also
remove this lobby thing bcz we all know this is lobby already"*, *"make sure all buttons work and
shit works right ennd to end"*, then, mid-pass: *"make these huds or ui look good bruh its so weird
to look at as none of them have visual harmony or shit"*, *"make sure all sfx play the right way"*,
*"i want thgis to say my name instead of YOU"*, *"i want u to make sure that everyone can see the
names in multiplayer (lan or server)"*, *"do u not feel weird that theres b ig ass empty space left
and right"*.

#### 68.18.1 Navigation: two screens left the path, and one panel was deleted

* PLAY goes straight to `MatchSetup` with `Networked = true`, so the landing state is the lobby
  auto-hosting on LAN. `ModeSelect` is unreferenced and **kept on disk and in the build order**,
  per § 68.3, alongside `MultiplayerSetup`. `SceneFlow.ModeSelect`'s own note carries the reasoning
  and `UiClickProbe` still probes both, because a fallback nobody checks is not a fallback.
* `MatchSetup`'s `CancelTarget` and BACK both go to `MainMenu` now. They have to agree or one of
  them is a step the other does not take.
* **The text tutorial is DELETED**, and it is the one place this batch departs from § 68.3's
  keep-the-old-chrome rule: that rule protects a REPLACEMENT that might turn out worse, and this
  was a deletion asked for by name with a shipped, played replacement. Gone:
  `ConvertedTutorialPanel.cs`, `TutorialContent.cs`, `Scenes/Ui/Tutorial.unity` and the
  `TutorialPanel` node inside `MainMenu.unity` (27 GameObjects, removed through the scene API).
  ⚠️ **The node had to go with the script.** A `MonoBehaviour` whose `m_Script` guid resolves to
  nothing is a yellow warning in the editor and a **refused build** under `SceneScriptCheck`, which
  is the only gate that can see it (`CLAUDE.md` § 7.1).
* The route moved to `SceneFlow.StartTraining`, because it was a private static on the deleted
  panel. `TUTORIAL` on the title screen enters it directly. `TutorialContent.ChipWidth` moved to
  `CreditsContent`, which is the only overlay left that draws a chip row.
* `DeadFeatureAudit` now asserts both halves: `SceneFlow` still arms `GuidedTutorial`, and the menu
  still reaches `StartTraining`. Either alone is silent.

#### 68.18.2 ⚠️⚠️ THE MATCH SETTINGS PANEL WAS UGLY FOR ONE MEASURABLE REASON

In `MatchSetup.unity` every caption is authored at **52 units** and every value at **34**, so the
word `MAP:` was drawn half again as large as the map's name. The label shouted and the thing it
labelled whispered. The rebuilt row inverts that (caption 22 amber, value 26 cream) and adds the
half nobody sees: **a fixed caption column**. `MAP:`, `MODE:` and `BOTS:` are three different
widths, so the authored `HorizontalLayoutGroup` started each stepper at a different x and nothing
in the panel lined up vertically.

* Every authored node is **restyled, never rebuilt** (§ 68.4). The arrows keep their
  `TextureButtonFeedback`, the values keep their `GodotOutline`, all of them keep their wiring.
* The colon is dropped, which is worth 54 px: the caption column has to hold the longest caption,
  and `BOTS` is 54 px narrower than `BOTS:`.
* CHARACTER left the panel entirely. See § 68.18.4.

#### 68.18.3 ⚠️⚠️ ONE RAIL PER SIDE, AND THAT IS A STRUCTURAL FIX RATHER THAN BETTER NUMBERS

Measured off `Logs/shots-runtime/Lobby-v35.png`, the bottom-left had **three left edges and three
widths**: the MATCH SETTINGS pill at x=75 running 300 px, its summary at x=60, START MATCH at x=55
running 380. The cause was that the left side was TWO hosts (`LeftColumn` at one anchor, a
`SettingsDrawer` beside it at another) at two different scales. **Two containers cannot share an
edge by arithmetic.** There is one `VerticalLayoutGroup` per side now and the group gives every
child the rail's width by construction.

* `LeftScale` 0.66 is **deleted**. It made every number on that side a lie: a 56 px header drew at
  37 and an 18 unit caption rendered at 12. The rail is authored at its real size.
* `LeftWidth` came down 560 → **460**, and the 100 px came out of the caption column rather than
  out of the type. At 460: 96 caption + 14 gap + a stepper of 20 padding, two 42 px arrows, two
  6 px gaps, leaving **214 px** for a value against `ILALIM NG TULAY` measuring about 195.
* The right-hand furniture **left `Columns` entirely**. `Columns` is a child of `Body`, a
  full-screen `VerticalLayoutGroup`: disabling the group ON `Columns` never stopped `Body` driving
  `Columns` itself, so "48 px in from my parent's right edge" was 48 px in from a moving rect. The
  old code compensated with a `-47` constant; `Lobby-v36.png` still had the pill 145 px from the
  edge against the chat's 48.
* ⚠️ **The lobby drawer stacks off `LobbyChat.PanelHeight`, not off its capacity.** The chat
  reserves six line slots and then collapses onto its content, so an empty log is about 65 px and
  the capacity expression gives 224. It is re-asked every frame because the chat grows as lines
  arrive; the guard makes that free.
* One harmony set decides every edge: `EdgeMargin` 48, `BottomMargin` 40, `TopMargin` 34,
  `RailSpacing` 12, `HeaderHeight` 56 (BACK and both tabs), `ToggleHeight` 52 (both drawers),
  `ActionHeight` 104.
* The three selector values are fitted **as a set**, to the largest size all three accept, and
  **reset to `ValueSize` on every pass**. Fitting them individually is why `Lobby-v35.png` has
  `ESKINITA` and `HARD` at full size and `HERO STRIKE` visibly smaller; `MenuKit.Fit` only shrinks,
  so a pass that measured a half-built rect pinned the type small permanently.
* The closed drawer's summary was **composed once inside `LobbyChrome.Apply`**, which runs before
  the screen's first `Refresh`, so it shipped the authored placeholder: `Lobby-v35.png` reads
  `ESKINITA · CAPTURE · NORMAL` on a lobby set to Hero Strike, and `CAPTURE` is not a mode this
  game has. It hangs off `Refresh` now.
* BACK moved to the top-left corner the banner vacated, and the banner is `SetActive(false)` in
  `Street` (not destroyed: `Refresh` still writes `BannerLabel`, and `Classic` keeps the pennant).
* The status line under START MATCH is **hidden unless it is an alert**. The four messages a player
  has to act on (refused port, dropped connection, relay refused, still connecting) still open it.

#### 68.18.4 The player card, and where character select went

CHARACTER left the match settings for a reason that is **authority, not tidiness**: MAP, MODE and
BOTS are greyed on every client by `RefreshLeaderControls`, so keeping CHARACTER as the fourth row
of a panel that greys out told three players in every four-player lobby that they could not pick a
fighter. It is the one choice on this screen that is always yours.

* The authored `CharacterButton` is **reparented**, keeping its name, its `Button`, its
  `GodotButton` skin and its handler. `OpenCharacterSelect` is untouched and still reveals
  `CharacterSelectPanel` in place. § 68.13 holds.
* The button gained two lines (character at 24, loadout at 18) because one line of
  `CHESKA · KALAWANG · CROCS ▸` at 24 units drew the name you chose at the same size as the slipper
  you did not think about.
* ⚠️ **`›` (U+203A), not `▸` (U+25B8) or `▶` (U+25B6), and `EDIT`, not `✎`.** Checked against
  Darumadrop One's own cmap: 525 glyphs, and it has none of `✎`, `✓`, `◀`, `▶` or `▸`. Unity's
  dynamic-font fallback draws them from a system font at a different weight and baseline.
* `CardWidth` is **330 and deliberately not `RightRailWidth` 392**. Matching the chat looked like
  the harmonious answer and left a visible hole: the gap is between the end of a short left-aligned
  string and an affordance pinned to the right edge, so only the width closes it. **The shared axis
  is the right edge, not the width.** 330 is measured against the worst case in the roster:
  `LOLA PACING` (11 chars, ~154 px at 24 against 244) and `DECADES TUNA  ·  TSINELAS` (25 chars,
  ~225 px at 18 against 244), not against `CHESKA`.

#### 68.18.5 The names, on every machine

* The local nameplate carried the pronoun `YOU`, so the three other people in the lobby saw
  `Matthew` over that body and its owner did not. It carries the name now; the `◀` marker still
  says which one is yours, and an unset name falls back to `YOU` rather than to the literal
  `Player` that `PlayerLabel`'s header already records four seats sharing.
* ⚠️⚠️ **A name edited in the lobby never travelled.** `NetSession.OnClientConnected` sends
  `IdentifyServerRpc(token, PlayerName, ...)` ONCE, on the frame the transport comes up, which was
  the whole story while the only editable field was in Settings on the title screen. The lobby card
  is editable while connected. `PublishName` re-sends `Identify` on commit: `LobbySession.Admit` is
  idempotent for a peer re-identifying under the same durable token (it copies the seat, the
  spectator flag and all three picks across and takes only the new name), which is the
  fast-reconnect path exercised on every relaunch. ⚠️ **No new message, so `ProtocolVersion` stays
  at 6** (§ 68.2, § 69.1).
* Every box that can hold a name already grows-then-fits: the plates size from the measured string
  up to a 420 px cap then shrink the type, the card's field is best-fit 13..20, and the seat rows
  go through `FitLine`. `Balance.PlayerNameMax` is 14.

#### 68.18.6 The cast's third character did not move, and no test could see it

🧑, with it circled: *"pic 3 doesnt have animations or move but everyone else does in here"*.
`LobbyCast.Poses` slot 2 asked for `holding-right`, which is a **carry POSE, not a performance**:
the rig's six `holding-*` clips are what the arm does while a tsinelas is in it, sampled by
`CharacterAnimator` as a state. It is a real clip on all twelve rigs, it has a length,
`SampleAnimation` succeeds, and it returns the same frame at every phase.

⚠️ **`PickPose` resolves by NAME, and a name that resolves is indistinguishable from a name that
animates.** So the pick is measured: `LobbyCast.MotionOf` samples the clip five times across its
length and takes the largest distance any transform travels. A hold measures 0.0 and a breathing
idle measures centimetres; `MotionFloor` is 1 cm. The table now asks for `interact-right` (the
pick-up reach, a real animation) and the check is the floor under it. Five samples rather than two,
because a clip whose first and last keys match is ordinary (`DanceClip` is built that way).

#### 68.18.7 The Ilalim ng Tulay pillars, and why a camera angle was not the fix

The lobby shot is 12.6 m out at a 32 degree lens, which puts the camera **inside the colonnade**
rather than outside it, and `SwayDegrees` 7 over 26 s walks it across the gaps: intermittent by
construction, which is why *"sometimes"* is the word in the report and why no still render was ever
going to settle it.

A per-map lobby yaw aimed down a clear lane would still swing into a pillar at the ends of the
sway, and widening the shot would undo `LobbyFieldOfView`'s finding about the outer two characters
distorting. What is actually wrong is that concrete is between the player and a face, so
`MapPreviewSurface.ClearSightlines` takes it out of the way for as long as it is: a ray from the
camera to each body's **head and chest** (two points, because a pillar is tall and thin and one
sample at chest height clears while the same pillar is still across the face) against every
renderer's AABB.

* ⚠️ `MaxOccluderSpan` 14 m, or **the sweep hides the road**: the floor slab's AABB contains the
  camera, so a ray enters it at t≈0. A viaduct pillar is about 1x1x8 and a jeepney 6 long.
* ⚠️ The hit must be in FRONT of the person: `Bounds.IntersectRay` reports a hit anywhere along an
  infinite ray, so without the distance test every building behind the cast would count.
* ⚠️ Adopted objects are excluded, or the arc's inner two characters delete the outer two: the cast
  is adopted into the SAME scene as the arena.
* ⚠️ **Renderers are disabled, not GameObjects**, which would fight `Park`/`Unpark` over the active
  flag they use to decide which map lights the world. The previous sweep is undone first, every
  time, or the street is stripped one pillar at a time. Rate limited to 6 sweeps a second, and the
  renderer list is cached per map because `GetComponentsInChildren` allocates per root per call.
* It is the LOBBY shot only. The practice screen is a picture OF the map; hiding a pillar there
  hides the thing being chosen.

#### 68.18.8 ⚠️⚠️ ONE PRESS WAS FIRING THE CLICK UP TO THREE TIMES

🧑: *"make sure all sfx play the right way"*. The UI click is added in three independent layers,
each individually correct and none aware of the others:

1. the CONTROL on pointer down (`GodotButton`, `ArrowButtonView`, `TextureButtonFeedback`);
2. the WIRING on click (`ConvertedScreen.WireOne`, so a screen cannot forget);
3. the HANDLER (`Cycle`, `TakeSeat`, `SelectTab`, both COPY buttons).

A map arrow has all three. `AudioDirector.PlayAtVaried` has **no dedupe** and `PlayAt` pins the
pitch at 1.0, so three copies of one 40 ms recording start in the same frame at the same position
and sum to about **+9.5 dB**, undecorrelated. It read as a clipped clack on the arrows and a doubled
one on every wood button, next to a clean single click on the runtime-built controls that only have
layer 1.

⚠️ **The fix is in `MenuSfx`, not at the call sites.** Deleting two of three layers is a nine-file
edit that leaves the rule written down nowhere and regresses to SILENCE the first time somebody
removes the wrong one. **One press is one sound** is a property of the sound layer: all three may
ask, and the first ask per cue per frame plays. Per cue, because a frame may legitimately carry a
click and an error. Per frame rather than per time window, because a time window would also swallow
a genuine fast second press on a map arrow.

Also: **a BACK button plays `ui_back` now**, the same as Escape always has. `ui_back.wav` and its
own mix entry exist because backing out is meant to be audibly distinct, and every BACK button in
the game was playing a plain click. `GodotButton.PressCue` is the field, `ConvertedScreen.WireOne`
sets it, and the wiring names the SAME cue the control does so the frame guard collapses them.
`DeadFeatureAudit.EveryMenuSoundGoesThroughTheOncePerFrameGuard` is the tripwire.

#### 68.18.9 What is verified, and what still is not

`dotnet test` 111 green. EditMode **188/188**. PlayMode `UiRuntimeShots`, `LobbyStyleProbe` and
`UiClickProbe` all green, which means every node the screen reaches by name resolves under BOTH
styles and BOTH tabs and no label draws outside its box in any of the four arms.
⚠️ `LobbyStyleProbe` caught one live defect in passing: `MiniSection`'s headings drew `SHARE THIS
LOBBY` in a **4 px box** in `Classic`, because the holder has no layout group so its preferred
width is zero, and `Street` had only ever hidden it by `Narrow` writing a width onto the column.
`Checks.RunAll` five green. `audit_wire_payloads.py` 47 named messages 0 mismatched;
`audit_request_call_sites.py` 43 entry points 0 unreachable. Renders at
`Logs/shots-runtime/Lobby-v41.png` and `LobbySettings-v41.png`.

#### 68.18.10 ✅ THE CHAT IS PROVEN, ON TWO REAL PROCESSES

🧑: *"does say something even work? can u even chat with people?"*. Fair question, because nothing
on either end said so: the send side printed `sent=True`, which only proves a message reached the
transport, and the receiving end drew a label and logged nothing. A run where the host relayed
correctly and a run where it dropped everything produced identical logs on both machines.
`LobbyChat.Add` logs the receipt now, for the reason `ConvertedScreen.WireOne` gives about menu
presses: in a shipped .exe a line in `Player.log` is the only way to tell "it never arrived" from
"it arrived and the panel did not draw it".

Two built players, one machine, `-tp-lobby` on 8910 and 8911, B joining A with
`-tp-lobbyjoin 127.0.0.1:8910 -tp-lobbychat hello_from_B`:

```
A (host)    [Net] 2 connected, seat 1
            [Chat] received from 'Matthew': hello_from_B
B (client)  [LobbyAuto] join result True
            [Net] connected as seat 2
            [LobbyAuto] chat 'hello_from_B' sent=True
            [Chat] received from 'Matthew': hello_from_B
```

**Both legs are in those six lines.** Client to host is B's send arriving on A. Host to client is B
receiving its OWN line back, which only reaches it through `HostRelayChat`'s
`SendNamedMessageToAll("ChatLine")`: `OnChatLineMsg` refuses the host, so A's copy comes from the
local `OnChatLine?.Invoke` beside it and B's comes off the wire. It also incidentally proves the
name work of § 68.18.5, because the line is attributed to `Matthew` rather than to `P2` or
`SOMEBODY`, which means `LobbySession.PeerById` had the real name at relay time.

⚠️ **This does NOT close § 68.14 step 7.** It closes 7.9 for the lobby. Host to leave to join in one
launch, the LAN/online toggle, a joiner's cast wearing the right character, the ready ticks, the
rejoin path and chat IN THE ARENA are all still unexercised, and the join here was driven by
`NetAutomationProbe` on one machine rather than by two people on two.

#### 68.18.11 The lobby log opens on demand

🧑: *"big empty sapce here for lobby and say something"*, then *"make it so that if u clcik chat u
see like the logs for it and who sent but it clsoes when u click out"*.

* ⚠️⚠️ **An empty log row is DEACTIVATED, not set to zero height.** Zeroing it was the obvious fix
  and it did half the job: a `VerticalLayoutGroup` puts its `spacing` between every pair of ACTIVE
  children whatever their heights are, so six zero-height rows still contributed six 4 px gaps.
  With 20 px of padding the idle panel was 44 px of nothing above a 44 px field.
* The lobby log is **closed by default** and opens on a click anywhere on the panel. The plate eats
  clicks by design, so `OnPointerClick` focuses the field rather than swallowing the press, which
  is also what makes "click chat" mean the whole panel rather than one 56 px strip of it.
* It closes when the field loses focus, **polled rather than evented**: an `InputField` losing
  focus to a click elsewhere on the canvas raises nothing here, so the only honest test is whether
  it still HAS focus. `Typing` is that test and the input reader already asks it.
* ⚠️ **An arriving line opens it anyway**, for `LobbyLogLife` 9 s. A log that only opened on a click
  would be silent about the one thing it exists for, because the message you most need to see is
  the one that arrives while you are looking at the cast. Focus overrides the timer, so a long
  message is never cut off mid-typing. This is the same shape the MATCH log already has.
* `FieldHeight` 44 → 56, so with the rows away the field IS the panel: at that moment the control
  is an invitation to type and it should look like one.


⚠️⚠️ **§ 68.14 STEP 7, THE TWO-PROCESS RUN, IS STILL THE THING THAT HAS NOT HAPPENED**, and this
pass adds one item to it: **the name published after connecting** (§ 68.18.5) has to be seen
arriving on the other machine's plate, in both directions, host and client.

---


### 68.19 The picker was bleeding, the chat still grew, and the roster had four slippers

Four things off one report, 🧑 2026-08-28: *"ui hella broken when i click character changer in
lobby"*, *"thoroughly overhaul and make the TSINELAS Model better because it looks so ugly"*,
*"add new slipper, alpombra"*, and *"chat lowk buns, it justt extends to 3 chats and u cant see
past that"*.

**1 · The character picker is an overlay now, not another piece of lobby furniture.** The lobby
builds its tabs, drawers and chat AT RUNTIME, after the authored `CharacterSelectPanel` exists, so
hierarchy order alone drew every one of them over the picker's backdrop: the screen you opened was
the picker with the lobby printed through it. `EnsureCharacterOverlayIsolation` gives the panel its
own `Canvas` with `overrideSorting` and `sortingOrder` 100, its own `GraphicRaycaster`, and a rect
stretched to the full screen; opening it also closes the join panel and moves it last among its
siblings, which keeps the rule true for any future decoration that does not make its own canvas.

**2 · The chat panel is a fixed two-line box with a door in it.** 🧑: *"i want u to not make the
chat extend anymore bcz theres empty sapce, js keep it at tthe size i sent and u can see other
chats by clicking it"*. ⚠⚠ **This supersedes § 68.18's growing-log bullets.** That pass made the
lobby log open on a click and on an arrival and then GROW through the seat rail one line at a
time, and it still discarded everything past the sixth message, so it was both in the way and
lossy. The panel is `LobbyVisibleLines` 2 rows plus its field, always that size. Clicking it opens
`LobbyChatLog`: a centred, scrollable log of the last `MaxHistory` 100 lines, parented to the ROOT
canvas at `sortingOrder` 90, dismissed by CLOSE, by Escape, or by clicking the shaded backdrop
(*"it clsoes when u click out"*). `LobbyLogLife` and the auto-open are gone with the growth they
served: the two compact rows are always drawn, so an arriving line is already on screen and
covering the cast to announce it would say nothing new. `FieldHeight` stays 56 and § 68.18's
`Debug.Log` receipt on every arrival stays exactly as it was.

Two traps paid for inside that overlay and both are silent failures:
* ⚠⚠ **The text component IS the scroll content, not a child of it.** A `ContentSizeFitter`
  measures the `ILayoutElement`s on its OWN object; an empty `RectTransform` with a `Text` child
  reports a preferred height of zero, so the content stays 0 px tall, the `ScrollRect` finds
  nothing to scroll, and the log opens showing one screenful with the wheel dead.
* ⚠ **`RectMask2D`, not `Mask`.** `Mask` needs the graphic to BE the mask, which would have thrown
  away the wooden inset the viewport draws.

**3 · The slipper roster is nine, and every new row is a licensed source model.** `TSINELAS` itself
is rebuilt: the drawing-derived mesh is still deleted (§ `Art_Direction.md` 4a) and entry 0 is now a
sourced, cleaned flip-flop. `SPARTAN`, `ALPOMBRA`, `PAMBAHAY`, `HEELS` and `SANDALS` join it, each
with its own three-stat FLIGHT / IMPACT / RECOVERY row, its own character-select description and
its own mesh. `tools/build_slipper_roster.py` is the Blender pass that produced them: one shoe per
prop, isolated where the source was a pair, normalised to the game's 0.432 m, recoloured to the
role-safe palette. Sources and their licences sit together in `Art/models/kits/footwear` with
`NEW_SLIPPER_LICENSES.txt`, which is the attribution compliance and must not be deleted.

⚠⚠ **THE ROSTER IS APPEND-ONLY AND THAT IS WHY THIS BUMPS THE PROTOCOL, 6 → 7.** A slipper pick
crosses the wire as an INDEX. Inserting a row above an existing one would make two peers render
different footwear for the same pick with nothing to report, and a build that knows nine rows must
not be told about a pick by a build that knows four. `TSINELAS` stays index 0 because every -1
fallback resolves to it and its row stays neutral. `HeadlessCheck` counts nine and
`ChatAndLobbyChromeTests` holds the version number beside it.

**4 · Nemu's kit reads as Nemu's, not as Kuro's.** 🧑: *"also fix nemu's character skill
descriptioins, make it sound cooler bcz it's all just Kuro's shit"*. GHOST STEP → PHANTOM VEIL,
KURO PROJECTION → ASTRAL HIJACK, SEANCE VOID → DEVOURING SEANCE, with the character blurb rewritten
to match. Names and copy only; no ability numbers moved, so `Balance` and every ability test are
untouched.

⚠ **What this pass did NOT do:** nobody has PLAYED the nine-slipper roster or the new log yet, and
the two-process run of § 68.14 step 7 still has not happened. The stats above are authored, not
measured; the first thing to check when the roster is played is whether `HEELS` at IMPACT 5 /
RECOVERY 1 is a real trade or just the best slipper.

---


## 69 · The game has no chat, in the lobby or in a match ⚠️ OPEN, PLANNED 2026-08-28

🧑, 2026-08-28: *"yea maybe add a chat to our game too that works in lobby and ingame"*.

Four people in a lobby have no way to say anything to each other, and four people in a match have
no way to call a play. Emotes exist (§ 38.3 put them on the wire) and they are not the same thing.

### 69.1 ⚠️⚠️ THIS IS THE ONE THING IN BOTH SECTIONS THAT MOVES `ProtocolVersion`, 5 → 6

A chat line is a named message that has never existed, so both machines must be rebuilt from this
branch or they refuse each other at approval. § 68.2 has the full reasoning. **Bump it once, in
this section, after § 68 has been verified at 5.**

### 69.2 Shape

* One named message, host-relayed: sender's seat, and the text. ⚠️ **The sender is NGO's
  authenticated client id, never a seat carried in the payload.** § 54 records exactly this: a
  field the host has to remember to ignore is a field that gets trusted, and `DeclareReady` was
  cut down to a single `bool` for it.
* ⚠️ **The host clamps length and rate.** § 38.9 found two request channels any client could
  flood; a text channel is the obvious third. A cap on characters and a minimum interval per
  peer, enforced host-side, not client-side.
* ⚠️ **The name is the one already in `LobbySeatInfo`**, not a second name field. There is exactly
  one identity per peer and it crossed the wire before this.
* `tools/audit_wire_payloads.py` must show the writer and the reader agreeing field for field:
  § 38.6 exists because netcode does not check that and a mismatch is silently misread bytes.

### 69.3 In the lobby

Bottom-left above the START button, in the wood set. Always visible, last few lines, entry field
below. No key needed to focus it: the lobby has no gameplay to steal a keystroke from.

### 69.4 In the match, where the rules are different

⚠️⚠️ **A CHAT FIELD THAT SWALLOWS MOVEMENT KEYS IS A WEAPON.** Enter opens it, Enter sends and
closes it, Escape cancels, and while it is open the gameplay input map is suspended.

⚠️ **THE INPUT MAP RULE APPLIES.** `CLAUDE.md` § 4: one control, one action, PER CONTEXT, and
`InputMapAndAbilityTests` asserts it. Chat is a THIRD context after gameplay and spectating, and
it is a narrowing of the same kind § 35.3 records: a player typing has no verbs, so its keys can
never collide with theirs. It goes in the input asset and in the rebinding panel like every other
key. It does not become a ninth `Keyboard.current` read outside the asset.

⚠️ **THE LOG FADES, THE HUD DOES NOT GROW.** `VISION.md` § 2 rule 5: a screenshot mid-fight must
still show the lata, the chalk and every player. Chat lines retire after a few seconds like
§ 46.3's banner rather than accumulating, and they sit clear of the ability deck, which is where
§ 46.1 and § 46.4 both found something drawn on top of something else.

⚠️ **A SPECTATOR CAN READ AND SEND.** They have no body and no seat, and their name is still in
the roster.

### 69.5 Done looks like

The two-process run of § 68.14 step 7.9, both directions, in the lobby and in the arena, plus
`audit_wire_payloads.py` and `audit_request_call_sites.py` green on the new message, plus an
EditMode test on the host-side clamp and rate limit.

---


---

## The archive index

One row per section that now lives in [`TODO_Archive.md`](TODO_Archive.md). Same numbers,
whole bodies, nothing deleted. **This table exists so that a pointer written anywhere in the
repository still lands on something**: follow it here, find the number, read it there.

| § | What it was |
|---|---|
| 93 | A held tsinelas "drifted" 0.084 m from the hand ✅ CLOSED 2026-09-05. ⚠️ It was never a carry regression: `CarryTests` subtracted `RestHeight` and not the `DrawnCentreOffset` that `RideAnchor` also applies, so it measured half a shoe and called it slack. The bound is unchanged at 0.05 m |
| 137 | The two-process harness § 135.7 said did not exist, and the tables it was blocking ✅ CLOSED 2026-09-04. ⚠️ Read § 137.2 before reaching for `UnityTransport`'s simulator: it is `[Obsolete]` with no effect here. Closes § 135.6, § 135.7's buildable half, § 136.4 and § 134.9 |
| 136 | F1 did three things at once in practice, and the whole `ui_*` sound family went back ✅ CLOSED 2026-09-04. § 136.4's touch control is built in § 137 |
| 135 | The tournament network pass: the baseline, and the three verbs that refuse in silence ✅ CLOSED 2026-09-04. ⚠️ § 135.7's premise about the harness is corrected in § 137.1; its two HUMAN-blocked parts are in `Attention.md`, not here |
| 131 | The suite became a gate, the tutorial got its glyphs, and a red that was never about steering ✅ CLOSED 2026-09-03 |
| 129 | Three faults off the first phone render, and the one that was invisible on a monitor ✅ CLOSED 2026-09-03. § 129.3's mechanism is § 130.9 |
| 90 | The impersonation guard, and telemetry ⚠️ 2026-08-30 |
| 91 | Phase 4: XP, levels and hero mastery ⚠️⚠️ 2026-08-30 |
| 92 | The account and career screens, rebuilt ⚠️⚠️ 2026-08-30 |
| 94 | Phase 4.5: quality control across phases 1 to 4 ⚠️⚠️ 2026-08-30 |
| 125 | Controller, touch and crossplay, built so that forgetting is impossible ⚠️⚠️ 2026-09-02, branch `ui-redesign` |
| 124 | The skills are aimed and drawn in their own hand, the tutorial stopped lying, and Zack stopped being Sean ⚠️⚠️ 2026-09-02, branch `ui-redesign` |
| 123 | The match settings go back to steppers, the shadow was retuned on the wrong axis, and a tab pair sat at half its neighbour's contrast ⚠️⚠️ 2026-09-02, branch `ui-redesign` |
| 122 | The black line everywhere, the picker goes back to wood, and the loadout moves to the hero ⚠️⚠️ 2026-09-02, branch `ui-redesign` |
| 120 | The buttons get a thickness, and the four screens § 119.11 left get finished ⚠️⚠️ 2026-09-02, branch `ui-redesign` |
| 117 | The front end was two design systems stacked, and the code-drawn one was the wrong one ⚠️⚠️ 2026-09-01, branch `ui-redesign` |
| 116 | The front end had one material and no focus state ⚠️⚠️ 2026-09-01, branch `ui-redesign` |
| 115 | Eight faults in one build, phases 11 and 12, and the door he could not find ⚠️⚠️ 2026-09-01 |
| 114 | The boot is four screens, the lobby is the home, and the colour dial is deleted ⚠️⚠️ 2026-09-01 |
| 113 | The clothes were not clothes, the screen was see-through, and the door was a chip ⚠️⚠️ 2026-09-01 |
| 112 | The base rig is naked now, and the custom character walks into a match ⚠️⚠️ 2026-08-31 |
| 111 | The build he opened: no studio mark, and the boot screen in the wrong unit space ⚠️⚠️ 2026-08-31 |
| 110 | The character maker gets a wardrobe, and the custom hero borrows a kit ⚠️⚠️ 2026-08-31 |
| 109 | Phase 6's last mile: the three-hour hang, and the presence state nothing had ever lit ⚠️⚠️ 2026-08-31 |
| 108 | The custom character had no screen, and two screens were drawn under the screen that opened them ⚠️⚠️ 2026-08-31 |
| 107 | Roster Integrity and the 3-Slot Custom Character Creator ⚠️⚠️ 2026-08-31 |
| 106 | Phases 5 and 6 finished: the free colour dial, and parties as queue tickets ⚠️⚠️ 2026-08-31 |
| 105 | Phase 9: one ladder, five tiers, Glicko-2 ⚠️⚠️ 2026-08-31 |
| 104 | Phase 8: the witnessed result, and the finding that the plan's design would have been theatre ⚠️⚠️ 2026-08-31 |
| 103 | Phase 7: QUICK MATCH as a rating-banded queue ⚠️⚠️ 2026-08-31 |
| 102 | Phase 6: friends, presence and blocking ⚠️⚠️ 2026-08-31 |
| 101 | Phase 5 continued: the banner on the wire, palettes on remote seats, and the colour picker ⚠️⚠️ 2026-08-31 |
| 100 | ⚠️⚠️ THE BOOT SCREEN'S ART WAS FITTED TO A FRAME NOBODY CAN SEE, AND THE COLUMN WAS SIZED AGAINST THE WINDOW INSTEAD OF AGAINST THE FORM |
| 99 | ⚠️⚠️ EVERY `sortingOrder` A CODE-BUILT SCREEN SET WAS SILENTLY IGNORED, AND § 92.7'S FIX NEVER WORKED |
| 98 | Phase 5 begins: the banner, and wiring the rewards nothing wore ⚠️⚠️ 2026-08-31 |
| 97 | The boot account screen, PUBG-shaped, with the guest escape ⚠️⚠️ 2026-08-31 |
| 95 | ✅ CLOSED: the four title-screen buttons overflowed their own artwork at 720p |
| 95c | CLOSED: the loading screen was a black rectangle for most of the boot |
| 71 | The 2026-08-29 report, and the two faults only a non-host could see |
| 73 | The rest of the 2026-08-29 batch: feel, audio, and the casts nobody could tell apart |
| 74 | Zack's shock trail has the hazard bug that was fixed everywhere else ✅ CLOSED 2026-08-29 |
| 75 | The slipper throw wind-up, and what was actually checked ✅ CLOSED 2026-08-29 |
| 76 | Holding the pickup key does not right the can in the tutorial ✅ CLOSED 2026-08-29 |
| 77 | The network deep-dive: the half of § 71.3 that was never applied, and a refusal that was never sent ✅ CLOSED 2026-08-29 |
| 78 | The two-machine acceptance test, run at last, and the batch it paid for |
| 79 | The 2026-08-29 evening batch: what he reported, what landed, and what is still open |
| 81 | ⚠️⚠️ THE PLAYMODE ARENA SUITE IS NOT A GATE ANY MORE, AND HERE IS THE EVIDENCE |
| 80 | The 2026-08-29 late batch, reported while § 79 was being fixed |
| 82 | The 2026-08-29 night batch: the match that was over before it started |
| 83 | The 2026-08-29 balance-and-controls batch, reported while § 82 was being pushed |
| 84 | The 2026-08-30 batch: twelve reports off the shipped build, and a lighting number read off a dead field |
| 85 | The 2026-08-30 AUDIO and VISUAL list, sent as one block |
| 86 | The spectator pause, and the 35 ms every non-host was standing behind |
| 87 | Every tsinelas rendered flat brown in first person, and the fix for it flattened the shading on all of them ✅ FIXED 2026-08-31 |
| 0 | Hero Strike is being reworked, and the plan is its own file |
| 8 | The abilities still look repetitive, and half the fix is not done |
| 9 | Ilalim ng Tulay dressing defects, reported off the 2026-08-25 player |
| 12 | Everything 🧑 found playing the 2026-08-26 build ✅ ALL CLOSED SAME DAY |
| 13 | Everything the 2026-08-26 evening build showed, and the pattern in it |
| 14 | The 4.69 player's second batch, shipped in `349b0171` |
| 15 | The 4.70 tutorial batch, and why four screenshots were one probe apart |
| 16 | The probe was never deterministic, and § 10 was closed on an argument |
| 17 | The bots are steeply sensitive to the frame step, and a 50 fps machine is in the bad band |
| 18 | HUD strings overflow their boxes, in more than one place |
| 19 | The powers were fifteen poses sharing one construction, at every layer |
| 20 | Cheska's kit played the wrong sounds, and every zone died in silence |
| 21 | Phaister merged in, and everything she arrived without |
| 22 | Everything the 4.71 player showed, and the two entries that were ticked but not wired |
| 23 | Ability stuns are now fought out of, not waited out |
| 24 | Phaister's three powers were one builder at three radii |
| 25 | Which peers actually hear a sound, measured rather than assumed |
| 26 | Every ultimate changes the weather, and each hero changes it differently |
| 27 | The other five heroes need a motif, and it is not more symbols |
| 28 | Nemu's ultimate is her pet now, and her kit is named after him |
| 29 | The other four heroes got their motif, and none of them shares a builder |
| 30 | Two findings from measuring the cue files, and one stale line in `CLAUDE.md` |
| 31 | Everything the 4.72 playtest reported, and the two faults it exposed |
| 32 | The networking was broken by one unreplicated static, and four other faults on top of it |
| 33 | The bots picked a target by seat number, aimed powers at rings they do not cast, and had no keyboard between decisions |
| 34 | Seat 0 was steered by a different movement model in every all-bots run, and it is § 11's second layer |
| 35 | The spectator flies itself, every key is in the panel, and a reconnect stops refunding cooldowns |
| 36 | The host never transmitted its own bodies, so a joiner saw three statues |
| 37 | Two Phaister presentation faults from the 4.72 player ✅ CLOSED, SEE § 43 |
| 38 | The network pass: eleven faults the host cannot see, and the loopback behind four of them |
| 39 | The settings wheel, for the fourth time, and the cause the first three missed |
| 40 | The train is one field recording now, and it plays rarely |
| 41 | The ultimate meter counts events now |
| 42 | Nemu's ride home was being erased by her own body's bot |
| 43 | Two Phaister presentation faults, and a class of fault behind one of them |
| 44 | § 32.3's slider fix was muted by the sweep on the next line ✅ CLOSED 2026-08-27 |
| 45 | The in-match HUD had five ambient sines, three copies of "LATA DOWN" and twelve coloured cells |
| 46 | Both intermission banners were drawn on top of something ✅ CLOSED 2026-08-27 |
| 47 | `Checks.RunAll` has been red since the Phaister merge, in two places |
| 48 | Kuro's projected body deleted itself mid-ability, and took Nemu's way home with it |
| 49 | Seat 0 travels about half what seats 1 to 3 do, in Classic, every run |
| 50 | Fourteen reports off the 4.73 player ✅ CLOSED 2026-08-27 |
| 51 | The four follow-ups off § 50 ✅ CLOSED 2026-08-27 |
| 52 | The ready and rematch gates counted a seat as a peer, and five guards allocated before they guarded |
| 53 | A joining client could not move, and the cause is that its keyboard was left on seat 0 |
| 54 | Which of the two lobby fixes was kept, and why |
| 55 | The lobby was a picture of a lobby ✅ CLOSED 2026-08-27 |
| 56 | What the merged network pass still leaves open |
| 57 | The match ends on one machine, and three other events never reach a client at all |
| 59 | Two machines could discover each other and could not join, and it is one missing string split |
| 60 | The host announces a seat twice, by two protocols, and only one of them does the job |
| 53 | The corner stamp is the branch name ✅ CLOSED 2026-08-27 |
| 62 | Losing the host left a client playing on alone, and § 60.1 did not fix the movement |
| 63 | A game could be joined exactly once per launch, and remote bodies never animated |
| 64 | The bots had no face, no feet, a perfect memory and one opinion |
| 65 | Hosting or joining a SECOND time in one launch was refused, silently |
| 66 | Joining bounced the view about, and rejoining a running match was impossible |
| 67 | What the HARRYDAKS merge was hiding, found by building it |
| 70 | The prop art was replaced wholesale, and IKE was never a bad model ✅ MOSTLY DONE 2026-08-28 |
| 1 | Peer rematch voting across the wire |
| 2 | Cheska's Ice Barricade duration was set by accident ✅ CLOSED 2026-08-25 |
| 3 | The five hero accents have not been seen in a real match |
| 4 | Bayan Plaza's monument stands inside the defender's box |
| 5 | The overclock window has not been measured against a match |
| 11 | Every probe number ever printed was an average over a seat that could not play ✅ CLOSED 2026-08-26 |
| 10 | `BotBehaviourProbe` cannot answer a comparison, and every open balance question is one |
| 6 | `AiDiagnosticProbe`'s Classic round is a real-time test and it flickers red |
| 7 | The test suite costs more to run than it is currently returning |
| 58 | The ink outline tore open at every hard edge ✅ FIXED 2026-08-27 |
| 63 | The world outline was aliased because MSAA was never able to see it |
| 63 | Walking into a utility pole blanks half the screen, and it now dithers away |
| 64 | The player can switch render styles, and the alternative is a chromatic look |
| 65 | The white keyline round every silhouette, measured rather than argued |
| - | Closed |
