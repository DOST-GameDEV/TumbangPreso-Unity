# FUTURE.md: turning Tumbang Preso into a game people come back to

**What this file is.** An ordered build plan for the live-service and competitive half of the
game: accounts, a database, profiles, stats, matchmaking, ranked, progression, unlockables,
customisation, controller, mobile, tournaments. Every phase carries a **PROMPT** block written to
be pasted straight into a new session as its whole brief. **Every prompt lives in § 19**, in
one place, with an index. § 0.5 is the standing preamble each one inherits, and § 0.6 is the
staleness protocol: what to re-verify before trusting any of them.

**What this file is not.** It is not a decision that any of it ships, it is not balance, and it is
not `docs/Design.md`. Where this file and `docs/VISION.md` disagree about what the game IS,
`VISION.md` wins.

⚠️ **Its companion is [`INSPIRATION.md`](INSPIRATION.md)**, which is the study of what other games
do and what each thing becomes here. This file is the WHAT and the WHEN; that one is the WHY, and
it carries eleven more prompts plus the combined order for both files in its § 8.6.

Written 2026-08-31, from 🧑's own words: *"I want this game to be an actual esport like league or
valorant or overwatch"*, *"competitive ranking, player profile, shit u can grind for like diff
skills"*, *"ppl can make an acct in it and there is a matchmaking system as well as different
ranks and different points"*, *"customizable characters in the future with unlockable skills"*,
*"stats too and shit as well as mobile + controller support"*, *"upload profile pic, add email,
add bio, show stats, match history"*, and the constraint that governs every line below: *"keep in
mind we dont have budget for paying for anything"*.

---

## 0 · Read this before picking a phase

### 0.1 The six rules that constrain everything here

1. ⚠️⚠️ **BOTH MODES SHIP AND CLASSIC NEVER GETS POWERS.** `VISION.md` § 1. A progression system
   that hands out abilities applies to **Hero Strike only**, or it deletes the reason Classic
   exists. Phase 10 is built around this and it is the easiest thing on this page to get wrong.
2. ⚠️⚠️ **NOTHING COSTS MONEY.** Every service named is a free tier that exists today, and each
   phase states what breaks first when that tier runs out. If something cannot be done free, it
   says so in its own words instead of being quietly skipped.
3. ⚠️⚠️ **THE GAME IS HOST-AUTHORITATIVE PEER TO PEER.** `MatchDirector.AddScore` runs on the host
   and the host is a player. **Ranked on that architecture is cheatable by the host.** Phase 8 is
   not decoration: it decides whether a rank means anything. Do not ship Phase 9 without it.
4. **The rules core stays engine-free.** `Packages/com.tumbangpreso.core/` must never acquire a
   `UnityEngine` reference (`CLAUDE.md` § 4). Rating maths, XP curves, unlock rules and season
   arithmetic belong there, where they can be asserted in a second instead of playtested for an
   afternoon. **Put the numbers there.**
5. **Every wire-facing list is append-only.** `Roster.Slippers` records why: an index that crosses
   the wire cannot be reordered without two peers rendering different things for the same pick,
   silently. Characters, cosmetics, ranks and seasons all inherit this.
6. **Nothing on a progression track may change a gameplay number.** Said once here and twice more
   below, because it is the single mistake that cannot be undone after players have ground for it.

### 0.2 What already exists, so no phase re-derives it

| | |
|---|---|
| Netcode | `com.unity.netcode.gameobjects` 2.13.1 over `UnityTransport`. `NetSession` owns it. |
| Online discovery | **UGS Lobby**, live. `ServerQuery` browses, heartbeats at 15 s, resolves 4-character join codes LAN-first then online. |
| Relay | **UGS Relay**, live, through `com.unity.services.multiplayer` 2.3.0. |
| Auth package | **`com.unity.services.authentication` 3.7.4, installed and IN USE.** ⚠️ This row read "unused" and was already wrong when Phase 1 started: `NetIdentity` had always signed in anonymously at boot. Phase 1 shipped on top of it, `docs/TODO.md` § 88. |
| LAN | `LanBeacon`, with persistent peer identity so a reconnecting player gets their seat back. |
| Reconnect | `LobbySession` already implements seat reclamation, a fast-reconnect window and leader election. **Do not rebuild this.** |
| Protocol gate | `NetSession.ProtocolVersion`, **16** as of 2026-08-30. Peers on different versions refuse each other at approval, by design. ⚠️ **Read the constant rather than this row**, per § 0.6: it has moved three times since this file was written (14 spectator pause, 15 match record, 16 the impersonation guard). |
| Bots | `AIController` plus `GameLaunch.AllBots`. A bot presses the same buttons a human does, one physics step serves both. |
| Spectating | `SpectatorCamera` with free, follow and POV modes, plus a spectator pause that crosses the wire. |
| Chat | `LobbyChat`, lobby and in-match, with hard-won layout notes. Extend it; never write a second one. |
| Content | 18 characters, 6 heroes, 6 lata, 10 tsinelas, 3 maps, `RosterBook` resolving id to model and palette. |
| Recolouring | `ToonSkin`'s 16-slot palette remap, per renderer, cached. **A colour variant of any character is already nearly free.** |
| Settings | `Settings.SettingsStore` for persistence, `Rebinding` for the input map. |
| Input | ⚠️ **REVERSED 2026-09-02.** Read `docs/TODO.md` § 125 and `CLAUDE.md` § 4a. This row said *"Keyboard and Mouse only. Zero gamepad bindings, zero touch bindings, no control schemes."* It is now **three control schemes, 26 gamepad bindings and a generated on-screen touch layer**, and a new `Verb` does not COMPILE without a pad binding and a thumb target. |
| Build targets | ⚠️ **Windows Standalone, WebGL, Linux Dedicated Server AND ANDROID**, as of 2026-09-02. Still **no iOS**: it needs a Mac the team does not have. |
| Localisation | **None, and it stays none.** English only, § 16.3. |
| Accessibility | **None beyond rebinding.** No colourblind mode, no UI scale, no subtitle system. |

⚠️ **THE FIXED VPS POOL IS RETIRED AND ANY NOTE SAYING OTHERWISE IS STALE.** `ServerQuery`'s header
records it: `139.180.212.110` ports 8910-8917 with a +10 status offset was the Godot
implementation, and UGS Lobby replaced it on 2026-08-19. Do not plan around that pool and do not
resurrect the UDP query loop.

### 0.3 The free-tier budget, named

| Service | Used for | What breaks first |
|---|---|---|
| UGS Authentication | Accounts, `PlayerId` | Nothing. Genuinely free at any size this game will see. |
| UGS Cloud Save | Profile, stats, inventory, progression | Item size, not user count. One small document per concern. |
| UGS Cloud Save Files | Avatars, replays | Per-player storage quota. Cap the upload size hard. |
| UGS Leaderboards | Ranked ladders, seasonal boards | Entries per board and reset frequency. |
| UGS Cloud Code | The only server-authoritative writer | Monthly invocations. **Call it once per match, never per event.** |
| UGS Lobby | Discovery, parties, the matchmaking queue | Queries per second. `ServerQuery.QueryInterval` is 4 s for exactly this reason. |
| UGS Relay | The connection itself | Concurrent users and GB. **This is the first thing that will actually bite.** |
| UGS Matchmaker | Skill-based queueing | Match requests per month. Phase 7 gives a zero-cost fallback. |
| UGS Analytics | ❌ **NOT USED.** Telemetry goes through Cloud Code instead | ⚠️ `docs/TODO.md` § 90.3 has the reasoning: the package cannot be added to this project's resolver state (`Net/CloudCode.cs`'s header records the same limit), and its custom events must be declared in the dashboard before they are processed, which would make every new event a manual step on an account only 🧑 can open. The Cloud Code path was already deployed, already probe-proven and already free. |
| GitHub | Repo, CI, releases | Nothing at this size. |
| itch.io | Distribution | Nothing. |

⚠️ **Multiplay (dedicated game servers) is the one thing on the shopping list that is not free.**
Everything below is arranged so that the day it is affordable, it slots in behind Phase 8.2
without rewriting anything else.

### 0.4 The order, and why it is this order

```
1  ACCOUNTS ────> 2  PROFILE + STATS ──> 3  TELEMETRY
                       │                      │
                       │          4.5 QUALITY CONTROL (1 to 4)
                       │
                       ├──> 4  PROGRESSION ──> 5  COSMETICS ──> 10 HERO MASTERY
                       ├──> 6  SOCIAL
                       ├──> 7  MATCHMAKING ──> 8  INTEGRITY ──> 9  RANKED
                       ├──> 11 BOTS + POPULATION
                       ├──> 12 MODES + MAPS
                       └──> 13 SEASONS + LIVE OPS

14 CONTROLLER ──> 15 MOBILE                (independent of the whole column above)
16 ACCESSIBILITY                           (independent, and overdue; localisation cut, § 16.3)
17 TOURNAMENT, LAN, SPECTATE, REPLAYS      (partly urgent: see § 17)
18 DISTRIBUTION                            (last, and smaller than it looks)
```

**Do 1, then 2, then 3, then stop and play it for a week.** Everything after that is worth more
when there is real data to point at.

⚠️⚠️ **THAT ADVICE WAS OVERRIDDEN ON 2026-08-30 AND PHASE 4 SHIPPED THE SAME DAY.** 🧑:
*"the testing goes in the ennd and its finne to go ahead of schedule"*. **The half of it that
still stands is the balance half**: every number in Phase 4 is a starting point, `docs/TODO.md`
§ 91 says so of each one, and the telemetry from § 90.3 is what should move them. Going ahead of
schedule is a decision about ORDER; it is not permission to call an unmeasured number balanced.

### 0.5 The standing preamble, which every prompt in both files inherits

⚠️⚠️ **A SESSION RUNNING ANY PROMPT IN `FUTURE.md` OR `INSPIRATION.md` IS BOUND BY THIS SECTION.**
Each prompt names it rather than repeating it, so there is one copy to fix when it changes and no
chance of nineteen copies drifting apart. **If you are a session that has just been handed a
prompt: this is part of your brief. Read it now.**

**1. Read order, before touching anything.**
`CLAUDE.md`, then `docs/VISION.md`, then `docs/TODO.md`, then this section, then the phase section
your prompt names. The summary in a prompt is never the rules.

**2. Verify before you trust.**
Every factual claim in these two documents was true on the date in the file header and may not be
now. § 0.6 lists the ones most likely to have moved. **Where this document and the code disagree,
the code is right.** Fix the document in the same commit and say so in your handoff.

**3. Where the code goes.**
Rules, curves, tables, thresholds, validation and any number that could be argued about go in
`Packages/com.tumbangpreso.core/`, which must never acquire a `UnityEngine` reference. The Unity
side is presentation, input and transport. If you find yourself writing an `if` about game rules
inside a `MonoBehaviour`, it is in the wrong file.

**4. Nothing on any progression track may change a gameplay number.**
Cosmetic or expressive only. Write the test that proves it.

**5. Wire-facing identity is string ids, and lists are append-only.**
`Roster.Slippers` records why an index that crosses the wire can never be reordered.

**6. Server-authoritative writes only.**
Profiles, stats, ratings, XP, currency and unlocks are written by a Cloud Code endpoint computed
from a match record, never sent by a client and never written by the host directly.

**7. Offline and LAN must keep working.**
Every feature degrades to a local profile when the network is unreachable. Practice, Training,
LAN and joining by code must never sit behind a login. See § 17 for why this is not negotiable.

**8. Free tier only.**
If the design needs a paid service, stop and say so in the handoff rather than building half of it.

**9. Definition of done, for every prompt.**
- The feature works and you have exercised it yourself, not reasoned that it should work.
- `dotnet test Core.Tests/TumbangPreso.Core.Tests.csproj` passes.
- EditMode passes. Assert on `Logs/tests.xml`, never on the exit code.
- The PlayMode probes that touch what you changed pass, run with `-testFilter`.
- ⚠️ **A test run that reports `total="0"` is not a pass.** Check the count, not just the result.
- New rules have new tests in the core.
- `docs/TODO.md` has a section for the work, in the same commit as the work.
- Any claim in `FUTURE.md` or `INSPIRATION.md` you found to be stale is corrected, same commit.
- Committed, pushed, and the handoff written in the chat reply per `CLAUDE.md` § 2.4.

**10. How to work while Unity runs.**
Every editor launch goes in the background and you keep coding. Never sit and watch a test run.
Never edit a `.cs` file while a Unity run is in flight. `CLAUDE.md` § 2.1b.

**11b. ⚠️⚠️ WHAT COUNTS AS A REASON TO CUT SOMETHING, BECAUSE IT IS NOT TEAM SIZE.**
Asked on 2026-08-31 which phases to drop on what they cost the team, 🧑 refused the question:
*"i have ai dont think abt 5 students shit"*, and *"the cutting shit i want should be focused onn
things that overcomplicate game for ppl"*. **So the test for cutting a feature is what the PLAYER
has to hold in their head, not what it costs to build.** A thing that is cheap to build is still a
candidate if it adds a bar, a screen, a number or a vocabulary the player has to learn; a thing
that is expensive survives if the player never has to think about it. `INSPIRATION.md` § 10.3 is
the register of everything cut on this basis and it names what was offered and DECLINED, which is
just as binding. ⚠️ **This does not license ignoring cost entirely.** Free-tier limits (rule 8) and
obligations a person has to carry, content moderation in particular, are real constraints and are
not the same argument as "this is a lot of work".

**11. What to do when the prompt is wrong.**
These prompts were written before the code they describe existed. If the design in one is
impossible, already done, or made obsolete by something that shipped since, **do not build it
anyway and do not silently skip it.** Do the part that still makes sense, write what changed into
`docs/TODO.md`, correct the prompt in the plan file, and put the disagreement at the top of your
handoff.

**12. ⚠️⚠️ EVERY PHASE DESIGNS ITS OWN SURFACE. A FEATURE WITHOUT A SCREEN IS NOT SHIPPED.**
🧑, 2026-08-30: *"can u get other phases to think abt how hud and ui should look on their
respective phases as well annd they can use visual hierarchy and visual journney meethods that
other games currenntly employ"*. **§ 0.5b is the method and it is not optional.** Do not write the
rules, the endpoint and the tests and leave "where the player sees it" to whoever comes next.
Phase 4 did exactly that and § 91.7 is the one-line entry that resulted; the screens then had to be
rebuilt twice (`docs/TODO.md` §§ 92 and 94.7).

### 0.5b How a phase designs its screen, because every phase has one

⚠️⚠️ **THIS SECTION IS RULE 12 AND IT EXISTS BECAUSE THE ALTERNATIVE HAS BEEN TRIED TWICE.**
Phases 1 to 4 each built rules, an endpoint and tests, and each left the screen until last. What
shipped was a panel doing six jobs, which 🧑 photographed and rejected (*"theres liek 20 shits at
once"*, `docs/TODO.md` § 92), and then a rebuilt screen whose values sat 1600 px from their labels,
which he rejected again (*"its so messy and ugly"*, § 94.7). **Both were designed the same way: by
the last person to touch the feature, at the end, without a method.** Here is the method.

**Answer these five, in the prompt's own words, before writing the screen.**

1. **What is the ONE thing on this screen?** Every screen has exactly one primary action or one
   headline fact, and everything else is smaller, quieter or further down. ⚠️ **If you cannot name
   it in four words, the screen is doing two jobs and is two screens.** The account panel offered
   SAVE PROFILE, LINK USERNAME, SIGN IN, DELETE ACCOUNT, PLAY AS GUEST and CLOSE at identical
   size, so nothing led.
2. **Where does the player arrive from, and where do they go next?** That is the visual journey,
   and it decides placement more than taste does. A thing reached from the menu belongs where the
   eye already is when it leaves the menu. ⚠️ **`docs/TODO.md` § 96 is this question unanswered**:
   the hub has exactly one door and the person who commissioned it never found it.
3. **What does it look like with NOTHING in it?** The empty state is the state most players see
   first and it is the one that gets designed last. A fresh career opened with fifteen rows of
   `0/0 (needs 10 throws)`. ⚠️ **`FUTURE.md` § 2.2: do not show a stat you will not defend.**
   Withhold the ROW, not just the number.
4. **What is the destructive thing, and is it a peer of the safe ones?** DELETE ACCOUNT sat
   between PLAY AS GUEST and CLOSE at the same size, one misclick from a lost career.
5. **How many things are on screen at once, and can any of them be shut?** ⚠️⚠️ **GROUPING
   WITHOUT COLLAPSING DOES NOT FIX A WALL OF NUMBERS**, it aligns it. § 92.3b.

**Four ordering tools, and they are the whole of visual hierarchy.** Use them in this order and
stop when the screen reads: **position** (top and left lead), **size** (the type scale, three
steps, never more), **weight and colour** (one accent, used for the one thing), **space** (a gap
groups more strongly than a line does). ⚠️ **COLOUR IS THE LAST RESORT AND NOT THE FIRST.**
`VISION.md` § 2 is a readability budget and this repository has a measured colourblind problem
(§ 16.1); a hierarchy carried by hue alone is a hierarchy some players do not have.

**What to copy from other games, which is a METHOD and not a screenshot.**

| Pattern | Who does it | What actually transfers |
|---|---|---|
| Settings as full-width rows, label left, control in a fixed column | Valorant, PUBG | **The column, not the alignment.** § 94.7 fault 1 is what copying the alignment without the column looks like. |
| One persistent action in a corner, per screen | PUBG | A screen with one button has an obvious thing to do; a screen with six has none. |
| Identity as a thing to LOOK at, then press | most live games | It states something before it offers something, which no button labelled ACCOUNT can. |
| A narrow form column beside art | Riot sign-in | Two questions, one primary, small footer links. |
| Progress shown where the player already looks | most live games | The level goes on the identity chip and the end-of-match board, not on a stats tab nobody opens. |
| Groups that collapse | every settings screen since 2015 | A group only helps if it can be shut. |

⚠️ **AND THE POINT OF THE TABLE IS THE RIGHT-HAND COLUMN.** Every one of those left-hand entries
was already "the reference" when the screens in § 92 were built, and they were still wrong,
because what was copied was the look. **Name the mechanism, then check whether this game's content
has the shape the mechanism assumes.** A wide dropdown fills a column; a two-character number does
not.

⚠️⚠️ **AND "IT WAS RENDERED" IS NOT THE SAME CLAIM AS "IT WAS RENDERED IN THE STATE A PLAYER
MEETS IT."** `CLAUDE.md` § 6.2b is the checklist and it exists because a screen with four green
renders at nine resolutions still shipped as a floating form over a lit menu with the nameplate
drawn across it. **Every state, over the real background, at the shape he actually plays at, with
every always-on piece of chrome still live.** A screen that appears unasked at boot is the one
place where "I could not get a picture of it" is not an acceptable answer.

**Moving around has to feel intuitive, and that is a separate test from any single screen.**
🧑, 2026-08-31: *"i want the user experience of movinng around the game to feel intuitive"*, and
the sharper version of the same brief: *"lets say im a player and i want to do something or find
something, make sure that entire experience feels great"*. **So the unit of design is the JOURNEY,
not the screen.** A screen that is beautiful and unreachable has failed.

**Walk the journey out loud before building any of it.** *"I want to X"* to *"X is done"*, naming
every press. If it takes more than three, or if one of them is a control the player has to
discover rather than read, the flow is the bug and no amount of layout fixes it.

| Rule | What it costs when it is broken |
|---|---|
| ⚠️⚠️ **Every destination has a visible door, and a door is a thing that LOOKS pressable.** | § 96: the hub had exactly one door, a corner chip that stated a name and a level and offered nothing, and **the person who commissioned the hub never found it.** |
| **Escape backs out, on every screen, always.** | `ConvertedScreen.CancelTarget` exists because three screens shipped with a dead Escape. **A player who learns Escape is reliable and then meets one screen where it is not has learned that it is unreliable**, which is worse than it never working. |
| **Escape closes the innermost thing first.** | One press, one layer. Closing everything throws away what they were reading; doing nothing traps them in a popup whose only exit is a button they have to find. |
| **A control that does nothing when pressed must not look pressable**, and one that does must react. | Four pennants scale and light up; the plate beside them did not move at all, so the only inert-looking thing on the title screen was the one door. |
| ⚠️ **Never add a second door to fix a findability problem.** | That is how the six-button panel in § 92 happened: a button per feature, in a different visual language, wherever its own offset put it. **Fix the door, or move it.** |
| **A dead end is a bug.** | A screen with no way back, or a button that dismisses to nothing, is `SignInScreen.OpenAtBoot` hiding BACK rather than shipping a dismissal to a black frame. |
| ⚠️ **The escape from a gate is one press and never needs the network.** | § 97. A boot gate is only acceptable because CONTINUE AS GUEST is one press and works with the cable out; that property has an assertion, not a paragraph. |

⚠️⚠️ **AND THE TEST FOR ALL OF IT IS A PERSON, NOT A PROBE.** `UiClickProbe.EveryButtonIsReachable`
can prove nothing is COVERED, and it has caught new chrome blocking a screen three times. **It
cannot tell you that a door nobody looks at is a door nobody finds.** Watch one launch, or ask
what they expected to press.

**What a phase owes before it may call its screen done.**

- **Built out of `UiRows`, never out of hand-written offsets.** That file exists because absolute Y
  offsets are a layout correct at exactly one panel height and one aspect ratio.
- **A layout probe at the nine resolutions** the other four UI probes use, asserting every label
  fits its box and clears `MenuKit.MinReadableUnits` (18). `PhaseSurfaceLayoutProbe` and
  `PlayerHubLayoutProbe` are the templates.
- ⚠️⚠️ **EVERY RECT ON IT ANSWERS `CLAUDE.md` § 6.2c's FOUR QUESTIONS**, which is the section
  written after 🧑 looked back at this whole run of work and said *"phase 1-4 had horrible ui
  integraitons"*. What is this size measured against; is this image fitted to the region it is
  seen in or to the whole screen; what is this dimming layer for and is that still true; and if I
  delete this, what else was it doing. **All four are faults that shipped here**, and not one of
  them is visible to a layout probe. `docs/TODO.md` § 100 is the entry: a 420-unit form inside a
  column sized at 38 per cent of the window, and key art cropped against a frame the column was
  covering a third of.
- **A render, looked at by a person.** ⚠️⚠️ **A GREEN LAYOUT PROBE IS NOT A GOOD SCREEN AND THIS
  IS THE MOST IMPORTANT LINE IN THIS SECTION.** § 4.5.3 says it and § 94.7 proves it: every one of
  seven readability faults was true while every label fitted its box and cleared the floor. **The
  probe asks whether the screen is a screen. The picture asks whether it can be read. Neither
  replaces the other.**
- **An entry in `docs/TODO.md` saying where every surface the phase produced went**, in the shape
  of § 92.4's table. It is what caught a shipped feature being deleted by a rebuild that had no
  reader left for it.

**The surface every remaining phase owes, and the ONE thing on it.** ⚠️ **These are the ANSWER TO
QUESTION 1 ONLY**, written here so no phase starts from a blank page. Questions 2 to 5 are still
that phase's work, and where a phase disagrees with this row it should say so and correct it, per
§ 0.5 rule 11.

| Phase | The surface it owes | The one thing on it | The trap it walks into |
|---|---|---|---|
| **5 · Cosmetics** | ⚠️ **CORRECTED 2026-08-31: the character SELECT screen, not a locker on the hub.** `docs/TODO.md` § 101.5 | **The character, wearing it.** A cosmetic screen with no preview is a list of nouns — and character select already IS that screen: the model, the real toon shader, the ink outline and the equipped palette are all on it. A locker on the hub would be a second screen showing the same character worse (§ 92), and the journey settles it: pick, recolour, done is three presses where you already are, against five that start by hunting a corner chip nobody has found (§ 96) | Phase 4's rewards are computed and worn by nothing (§ 91.8). ⚠️ **Wire the EXISTING rewards before authoring one new one**, or the first thing this phase ships is a second unworn set. ⚠️⚠️ **And check that they CAN be worn**: § 101.1 found `LoadoutRules.PaletteFor` refusing every input there is, because a mastery reward's id carries the hero and the variant table only knew the bare suffix |
| **6 · Social** | ✅ **SHIPPED**: a FRIENDS tab on the hub, and an ADD per human on the end-of-match board. ⚠️ **A TAB, NOT A GROUP ON `PROFILE`**, corrected 2026-08-31: a group is collapsed by default like every other one, and PROFILE is the screen about YOU while this is the only screen in the game about anybody else (`docs/TODO.md` § 102) | **Who is online now.** Everything else is a submenu — so the tab's subtitle is the answer as a sentence (`3 of 12 online`) and `SocialRules.Sorted` floats joinable and online to the top on their own | A friends list is a live list, so it has three empty states (no friends, none online, service down) and § 0.5b question 3 says all three get designed. ⚠️ **All three are built**, and they say different things: no friends points at the end-of-match board, none online is not an error and does not read as one, and not signed in is the guest's state and the only one with an action attached |
| **7 · Matchmaking** ✅ **SHIPPED** | A queue state on the LOBBY, not on a mode screen: nothing has navigated to `ConvertedModeSelect` since § 68.5 and PLAY lands straight on the lobby. `UI/QueueCard.cs`, `docs/TODO.md` § 103.3 | **Whether you are in the queue, readable from across the room** | A spinner is not a state. Say the mode, the time elapsed, and how to cancel, and never block the menu behind it |
| **8 · Integrity** ✅ **SHIPPED** | Almost nothing, deliberately: one line on the end-of-match board and no moderation console. `docs/TODO.md` § 104.6 | **A result that is disputed says so, once** | ⚠️ Resist building a moderation console. This phase's success is invisible |
| **9 · Ranked** ✅ **SHIPPED** | A tier WORD beside the level on the nameplate and a tier line on the end-of-match board. ⚠️  **No rating is drawn anywhere**, and the level keeps its `LV` prefix so the two can never be read as one quantity. `docs/TODO.md` § 105.6 | **Which way the number moved, and by how much** | The rank badge is absent on purpose today (§ 92.8) and level and rank must never be confusable. ⚠️ A bot-filled ranked match must SAY so on that board (§ 11) |
| **10 · Loadouts** | A loadout on the character screen; achievements as a CAREER group | **The three things you have chosen**, not the hundred you have not | Achievements are a wall of rows by nature. They collapse, and the sample-size rule (§ 2.2) applies to their progress numbers too |
| **11 · Bots** | A label in the lobby, on the scoreboard and in the match history | **That this seat is not a person** | It has to survive a screenshot. A grey name is not a label |
| **12 · Modes and maps** | The existing mode and map select, extended | **What is different about this mode, in one line** | Two modes fit as buttons; five need the collapse pattern before the fifth arrives, not after |
| **13 · Seasons** | One line on the menu, dismissible | **What is new, once** | ⚠️ This is the phase most likely to grow a popup on boot. It must not |
| **14 · Controller** | No new screen: glyphs everywhere there is a key prompt | **The button you are actually holding** | Every prompt in the game is a hard-coded key string today. Find them all before designing anything |
| **15 · Mobile** | Every screen again, at a thumb's reach | **The two things a thumb can hit** | ⚠️ `UiRows` assumes a pointer and a wide row. It is the file this phase renegotiates first |
| **16 · Accessibility** | A settings group, and a change to every other screen | **That the game is still readable with the colour turned off** | § 16.1 is a measured problem in THIS game. Hierarchy carried by hue is what this phase has to undo |
| **17 · Tournaments** | A bracket, and a spectator HUD that is not the player HUD | **Who is playing and what the score is** | A spectator has no body and no seat (`CLAUDE.md` § 4). Their HUD is a different screen, not the same one with pieces hidden |
| **18 · Getting it seen** | A screenshot that is not the game | **One frame that reads at thumbnail size** | Everything above optimises for a player two feet away. This optimises for a stranger scrolling |

### 0.6 How these documents go stale, and what to re-verify

⚠️⚠️ **THESE ARE PLANS WRITTEN AHEAD OF THE WORK. THE FURTHER YOU ARE FROM 2026-08-31 THE LESS OF
THIS IS TRUE.** The prose about design intent ages well. The claims about the codebase do not.

**Re-verify these before acting on any prompt.** Each is one command or one file.

| Claim in these documents | How to check it in one step | If it moved |
|---|---|---|
| Authentication and the account layer are present | `grep authentication Packages/manifest.json`, then `grep -r AuthenticationService Assets`, then `grep -r PlayerAccount Assets` | Phase 1 may have moved. Read `docs/TODO.md` § 88 before changing it. |
| The career layer is present, and `match-record` is deployed | `grep -r CareerStore Assets`, then `ugs cloud-code scripts list` | Phase 2 may have moved. Read `docs/TODO.md` § 89 first, and § 89.6 before touching `ProfileRules`, which is written in C# and again in JS. |
| Telemetry is present, and `telemetry` is deployed | `grep -r TelemetrySink Assets`, then `ugs cloud-code scripts list` | Phase 3 has shipped. Read `docs/TODO.md` § 90.3 before touching an event NAME: renaming one is a broken history and nothing errors. |
| A claimed lobby handle is verified | `grep -n VerifiedArrivalHandle Packages/com.tumbangpreso.core/Runtime/AccountRules.cs` | The impersonation guard is built, `docs/TODO.md` § 90.1. ⚠️ § 88.1c's prescribed fix is NOT what shipped and that entry says so; read § 90.1. |
| Every Cloud Code call goes through one helper | `grep -rn "cloud-code.services.api.unity.com" Assets` returns exactly `Net/CloudCode.cs` | A second hand-written request has appeared. `docs/TODO.md` § 89.5 records why that is the shape where the probe passes and the game fails. |
| Discovery is UGS Lobby, connection is UGS Relay | Read the header of `Assets/TumbangPreso/Runtime/Net/ServerQuery.cs` | The whole of §§ 0.3, 7 and 8 assumes UGS. Re-cost them. |
| ⚠️ **STALE SINCE 2026-09-02:** the input map has no gamepad or touch bindings | `grep -c Gamepad Assets/TumbangPreso/Resources/TumbangPreso.inputactions` returns **26**, not 0 | Phase 14 is SHIPPED and Phase 15 is playable. This row is kept because it is the check that proves it, and because it is the exact form § 0.6 asks for: read the code, not the prose. |
| ⚠️ **STALE SINCE 2026-09-02:** build targets are Windows, WebGL, Linux server only | `ls "/c/Program Files/Unity/Hub/Editor/*/Editor/Data/PlaybackEngines/"` shows **AndroidPlayer** | Phase 15 step 1 is done. ⚠️ **It is per MACHINE, not per repo**: the module is an editor install, so a fresh laptop shows the old answer and has to install it again. |
| There is no colourblind support and no UI scale | `grep -rn "colourblind|colorblind|UiScale" --include=*.cs Assets` | Phase 16 shrinks. |
| Progression exists, and XP is awarded server-side | `grep -rn ProgressionRules Packages/com.tumbangpreso.core`, then `grep -n award ugs/cloud-code/match-record.js` | Phase 4 has shipped. Read `docs/TODO.md` § 91 before touching a rate, and § 91.5 before moving the award to a second call site. |
| The account and career screens are `PlayerHub` | `grep -rn "class PlayerHub" Assets` | `AccountOverlay` and `ProfileOverlay` are DELETED, not deactivated. `docs/TODO.md` § 92. Build new settings-shaped screens out of `UiRows`, never out of hand-written Y offsets. |
| The roster is 18 characters, 6 lata, 10 tsinelas, 3 maps | `Packages/com.tumbangpreso.core/Runtime/Roster.cs` | Every content count in these files is wrong. Fix them. |
| Scoring is one host-side writer | `grep -n AddScore Assets/TumbangPreso/Runtime/MatchDirector.cs` | § 8's corroboration design may no longer be the right shape. |
| `NetSession.ProtocolVersion` is a gate between builds | `grep -n ProtocolVersion Assets/TumbangPreso/Runtime/Net/NetSession.cs` | Read the current number rather than quoting one from here. |
| The free tiers named in § 0.3 still exist at those shapes | Check the service's own pricing page | ⚠️ **Vendor free tiers change without notice. Never quote a specific quota from this file to anybody.** |
| ⚠️⚠️ **A GREEN FULL PLAYMODE RUN IS A GATE** | Run it twice and diff the failure sets | **It is not, as of 2026-09-03.** `docs/TODO.md` § 126.8: two runs of nearly the same code came back 42 red and 41 red **with eleven suites swapping sides**. Nine suites gave ~20 failures in the full run and **2** on their own. Verify with `-testFilter` over what you touched, which is what § 0.5 rule 9 already says, and treat the full run as a survey until § 126.8 is closed. |
| ⚠️ **The .apk has never been built and nothing has run on a device** | `ls ~/Desktop/TumbangPreso-Android/`, then `Logs/shots-android/` | **Done 2026-09-03**, `docs/TODO.md` § 126.10. ⚠️ The .apk is **arm64-only**: Unity 6 ignores the x86_64 request, and the emulator runs it by translation. Performance on that emulator is not a measurement (1 core, GPU off, translating). |
| Passive defence pays 900 a round against 100 for a knockdown | `docs/Design.md` and `Balance.cs` | Arguments in `INSPIRATION.md` §§ 2.15 and 4.2 rest on this. |

⚠️ **AND THE NUMBERS IN THESE FILES ARE ILLUSTRATIONS, NOT BALANCE.** Every rating step, XP curve,
tier name, challenge target, band width and threshold written here is a starting point for a
measurement, not a value to ship. `docs/Design.md` is the balance source of truth and nothing in
these two files may contradict it without going through `Design.md` first.

**Maintenance rule.** When a phase ships, mark its heading `✅ SHIPPED <date>`, move its numbers
into `docs/Design.md` or `docs/TODO.md` where they belong, and leave the phase text in place as the
record of why it is shaped the way it is. **Do not delete a shipped phase.** The reasoning is the
part that stays valuable.


---

## PHASE 1 · ACCOUNTS AND IDENTITY ✅ SHIPPED 2026-08-30

**The first part of the overhaul, in his words.** Everything else keys off a stable player id.

**What existed when work began:** `com.unity.services.authentication` 3.7.4 was already active,
not unused. `NetIdentity` silently signed in anonymously at boot, persisted the UGS session,
cached one attempt per process and degraded to a local token for LAN. `UgsCheck` exercised the
same path. `docs/TODO.md` § 88 is the as-built account record.

### 1.1 Sign-in, in the order that does not annoy anybody

- **Anonymous on first launch, silently, before the menu is interactive.** A player who never
  makes an account still gets a `PlayerId`, a profile and progression. **This is the whole trick:
  never block a first-time player on a form.**
- **Upgrade to a real account later**, offered at the moment they first earn something worth
  keeping rather than on a settings page nobody opens. Username plus password
  (`AddUsernamePasswordAsync`) needs no mail server and therefore costs nothing.
- **Sign in on a second device** by username, migrating the anonymous progress across.
- **Session persistence** so a returning player is signed in before the menu draws.

### 1.2 Email, and the honest version of it

🧑 asked for email. Be clear about what it buys and what it costs:

- **UGS username and password does not do email recovery.** There is no built-in "forgot
  password" mail. Adding one needs a transactional mail sender, and the free tiers that exist are
  Brevo at 300 mails a day and Resend at 3000 a month. Either is enough forever at this size.
- **So: username and password first, email as an OPTIONAL recovery field second.** Store the
  address in the profile, verify it with a code, and use it only for password reset and for a
  season-end summary if he ever wants one. **Never make it required to play**, because a required
  email at first launch is measurably the largest drop-off point any game of this size has.
- ⚠️ **An email address is personal data and this changes the obligations.** Once one address is
  stored, account deletion stops being a nice-to-have. Build it in this phase,
  where it is an afternoon, rather than after launch, where it is a migration. ❌ **Data export is
  cut** until somebody actually asks for it: § 1.5.

### 1.3 The identity fields, in full

| Field | Rule |
|---|---|
| `PlayerId` | UGS, immutable, never shown. |
| Username | Sign-in credential, unique, immutable after creation. |
| Display name | 3 to **14** characters, **not unique**. ⚠️ 14, not the 16 this table said until 2026-08-31: `Balance.PlayerNameMax` is 14, `LanBeacon` truncates the broadcast name to it and `Hud`'s row was measured against that many "W"s, so 16 rendered past a measured layout and arrived over LAN clipped. `AccountRules.DisplayNameMax` is now that same constant. ⚠️ The 30-day rename cooldown and the profanity filter are **written here and not built**; nothing rate-limits a rename today. |
| Discriminator | A 4-digit tag appended to the display name so uniqueness is not needed. `MATTHEW#4417`. |
| Email | Optional, verified, recovery and nothing else. |
| Bio | 140 characters, filtered, reportable, off by default until the player writes one. |
| Country flag | Optional, chosen not detected. Matters for a regional esport and it is free. |
| Pronouns | Optional, from a short list plus a free field. Cheap, and it costs nothing to be decent. |
| Avatar | See 1.4. |
| Created date | Shown on the profile. Founding players like knowing they were early. |

❌ **NO PRIVACY SETTINGS. CUT 2026-08-31.** The field list had a three-way visibility choice over
three kinds of data, which is nine states to build and test, on a game whose whole competitive half
depends on people being able to look each other up. **Profiles and match history are public**,
which is what every competitive game does and what the banner, the leaderboards and the
compare-with-a-friend feature all assume.

### 1.4 The profile picture, and a strong recommendation

🧑 asked for photo upload. **Do the in-game avatar builder first and the photo upload second, or
possibly never.** The reasons are practical rather than squeamish:

- An uploaded image is **content moderation**, permanently, run by five students. The first time
  somebody uploads something vile it is on a nameplate in a tournament stream.
- It is **storage and bandwidth** against a free tier, forever, growing.
- It **fights the art direction**. A JPEG face beside the voxel cast and the nine-patch UI looks
  like a bug.

**The avatar builder gets 90 per cent of the want at 5 per cent of the cost**, and this codebase is
unusually ready for it: pick a character, a pose, a palette, a background and a frame, and the
game renders the portrait locally from those ids. **The avatar is five integers on the wire**, it
needs no storage, no upload, no moderation and no bandwidth, it looks like the game, and it
becomes a Phase 5 unlockable surface immediately. `ModelPreview` already renders exactly this.

**If real uploads are still wanted after that:** cap at 256x256 and 100 KB, put them in Cloud Save
Files, allow them only for accounts with a verified email, show them only to friends until a
player has finished 10 matches, and put a report button on every one. Write that policy down
before the first upload, not after.

### 1.5 Traps

- ⚠️⚠️ **THE LOBBY NAME NOW COMES FROM THE PROFILE, AND IT IS STILL NOT IMPERSONATION-PROOF.**
  Half of this trap is closed: every hello, identify and beacon reads `PlayerAccount.LobbyName`,
  and `AccountRules.ArrivalHandle` validates what arrives. The other half is **open**, and the
  first attempt at it was backwards: it rewrote an honest bare name to `Player#tag` while
  admitting a fully claimed `Maria Clara#4417` verbatim, so it punished LAN peers and waved the
  actual forgery through. **A peer-hosted lobby cannot verify a claimed handle by itself**,
  because a real tag is allocated by UGS Player Names and the host cannot recompute it. Closing it
  needs the host to ask the `player-account` endpoint whether a player id owns a handle, and to
  fall through to the claim on LAN or when the endpoint is unreachable, per § 0.5 rule 7.
  `docs/TODO.md` § 88.1c is the entry.
- ⚠️ **Anonymous credentials live in the UGS authentication cache and a player who clears them is gone forever.**
  Say so in the UI at the moment they earn their first unlock.
- ⚠️ **Offline must still boot.** The game is played daily off a Windows build, sometimes with no
  connection. A failed sign-in degrades to a local profile and a visible "not signed in" state; it
  never blocks Practice, Training or LAN.
- ⚠️ **Age.** If under-13s play, storing an email brings COPPA-shaped obligations. The safe design
  is the one above: email optional, and no email at all for an account that has not confirmed an
  age gate.

**Done looks like:** a fresh install reaches the menu signed in with no prompt, the id survives a
restart, a username can be attached later without losing anything, an account can be deleted,
an offline tournament guest can enter without replacing the owner's account, and pulling the
network cable still lets a LAN match start.

**The prompt for this phase is [§ 19.1](#191-prompt-for-phase-1).** Every prompt in
this file lives in § 19 so there is one place to copy from. § 0.5 is the standing preamble each
one inherits and § 0.6 is what to re-verify before trusting any of them.

---

## PHASE 2 · THE PROFILE, THE STATS AND THE MATCH HISTORY ✅ SHIPPED 2026-08-30

**Why second:** a rank with nothing under it is a number. Stats are the cheapest retention feature
in the game and they make every later balance argument answerable.

### 2.1 The profile screen, laid out

✅ **SHIPPED 2026-08-30, WITH FOUR ITEMS DELIBERATELY LEFT OUT.**
⚠️ **IT SHIPPED AS `ProfileOverlay` AND THAT CLASS NO LONGER EXISTS.** The career page is the
CAREER and MATCHES tabs of `PlayerHub` since 2026-08-30; `docs/TODO.md` § 92 has the five faults
🧑 photographed and what replaced them. The four items below are still absent for the same
reasons. The header
card, the career strip, the mode tabs, the stat blocks, the paged match history and the match
detail are on screen. The **avatar** waits on § 1.4, which is still an open argument nobody has
answered; the **rank badge and peak** wait on Phase 9; the **achievement shelf** waits on Phase
10; and **compare** waits on Phase 6, because there are no friends to compare against yet. Each
would be an empty box, and an empty rank badge on every account in the game teaches every player
that the game has a rank. `docs/TODO.md` § 89.4 is the list with the phase that fills each one.
⚠️ **Level and border are not in that list**: the FIELD shipped and only the awarding did not, so
Phase 4 fills it with no migration.

1. **Header card.** Avatar, display name and tag, country flag, title, level and border, **the
   rank badge**, peak rank, account age. Designed so one screenshot is the whole flex.
   ⚠️ Read "rank badge per mode" until 2026-08-31, when the second ladder was cut: there is **one**
   rank now and the other mode is unranked. § 9. Mode tabs in item 3 are about STATS, which stay
   split by mode, and that is a different thing from having two ranks.
2. **Career strip.** Matches played, matches won, win rate, hours, favourite character, favourite
   tsinelas, longest win streak.
3. **Mode tabs.** Classic and Hero Strike, never merged, because they are separate games.
4. **Stat blocks**, filterable by season, by last 20 matches, and by character.
5. **Match history.** Twenty rows, paged, each row: mode, map, placement, score, character,
   duration, date, and a coloured left edge for placement. Clicking opens the detail.
6. **Match detail.** Full four-player scoreboard, per-round breakdown, who was taya each round,
   every player's per-stat line, and a replay link once Phase 17 exists.
7. **Mastery grid: six hero tiles**, each with level, win rate and games, plus a plain played-count
   list for the other twelve. ⚠️ Eighteen mastery tiles until 2026-08-31; § 10 records why the
   paths narrowed to the heroes.
8. **Achievement and highlight shelf.** The rare things, pinned by the player.
9. **Compare.** Put a friend's numbers beside yours. Cheap to build, and it is what makes a stat
   page get shared instead of read once.

### 2.2 The stats worth tracking, and why each one earns its row

| Stat | Why it is not filler |
|---|---|
| Knockdowns, and knockdowns per throw | The headline attacker number, with the rate so volume does not fake it. |
| Retrievals, and retrievals under pressure | `VISION.md` § 0: the tension is the retrieval. This measures the actual game. |
| Tags as taya, and tags per round defended | The defender headline. Fair by construction because everyone defends. |
| Passive defence seconds | The known balance risk, on the record where it can be argued about. |
| Sabotages | The third verb, otherwise invisible. |
| Average time to first throw | Reads aggression, and separates two players on the same score. |
| Longest survival as last attacker | The clip-worthy one. |
| Shove and lunge hit rate | The skill floor of the melee game. |
| Distance travelled per round | Reads playstyle: campers and runners look different here. |
| Per-character and per-tsinelas win rate | Feeds Phase 5 and every balance pass forever. |
| Placement distribution | A 4-player game has four outcomes, not two. Show all four. |
| Clutch rate | Matches won from last place at the final round. The stat people brag about. |

⚠️ **DO NOT SHOW A STAT YOU WILL NOT DEFEND.** Every number on a public profile becomes an
argument in a lobby. If a stat is noisy at low sample size, hide it until the sample supports it
and say why.

### 2.3 Where it lives

- `PlayerProfile` in **UGS Cloud Save**: identity, level, XP, career totals, per-mode records,
  per-character records, inventory, rank.
- `MatchRecord` written **once per match, by one writer**, carrying the whole scoreboard.
- ⚠️ **The host writes the record and that is a known hole, not an oversight.** Fine for unranked.
  Phase 8 closes it before ranked exists. **Write it through a Cloud Code endpoint from day one**
  even though it is spoofable now, because retro-fitting the call site later is the expensive half.
- Match history retention: keep 100 full records per player and roll the rest into the totals.
  Storage is the free tier's real limit and 100 is far more than anybody scrolls.

⚠️⚠️ **CORRECTED 2026-08-30, WHEN THIS SHIPPED: THE HOST AUTHORS THE RECORD BUT DOES NOT
SUBMIT FOR ANYBODY ELSE.** The hole named above is real and is still open, and it is exactly the
one described: the host counts the match and can lie about every number in it. What did NOT ship
is the host WRITING four career documents, which would have been a second and much worse hole:
the endpoint would have to accept any player id its caller named, so a host could rewrite a
stranger's career forever. The record is broadcast to every peer and each peer submits its own
line from its own authenticated session, so the cost is **one endpoint call per player per
match** rather than one per match. `docs/TODO.md` § 89.3 carries the full argument.

**Done looks like:** finishing a match writes exactly one record, the profile updates without a
reload, career totals survive a reinstall on the same account, and the whole thing is one Cloud
Code invocation per player per match rather than one per event (see the correction above).

**The prompt for this phase is [§ 19.2](#192-prompt-for-phase-2).** Every prompt in
this file lives in § 19 so there is one place to copy from. § 0.5 is the standing preamble each
one inherits and § 0.6 is what to re-verify before trusting any of them.

---

## PHASE 3 · TELEMETRY, EARLY ON PURPOSE ✅ SHIPPED 2026-08-30

⚠️⚠️ **`docs/TODO.md` § 90.3 IS THE ENTRY AND IT CARRIES THE EVENT-NAME CONTRACT. READ IT BEFORE
TOUCHING A NAME.** Two things below turned out differently and are recorded there rather than
silently skipped, per § 0.5 rule 11:

- **It does not use UGS Analytics.** § 0.3's row is corrected. The package cannot be added to this
  project's resolver state, and its custom events need declaring in the dashboard before they are
  processed. Telemetry goes through a third Cloud Code script instead.
- **Queue times by rating band are not built**, because there is no queue and no rating; Phases 7
  and 9 own both. `first_queue` is the honest substitute for the funnel step: the first time
  somebody opens the MULTIPLAYER screen. ⚠️ Do not later read that step as a queue time.
- **The FPS distribution is ✅ SHIPPED 2026-08-30**, and `docs/TODO.md` § 90.7 is the entry. It is
  the per-match sampler this bullet asked for: the window is `RoundActive`, so the splash, the
  menu, character select, the gaps between rounds and the results board are all outside it, and it
  is sampled on every peer rather than on the host, because a host-only frame rate is one machine's
  number reported four times. It sends an average, a median, a 5 per cent low and a 1 per cent low,
  and a band whose edges are `docs/TODO.md` § 17's cliff at 50 fps rather than round numbers.
  ⚠️ **THE LOWS ARE THE POINT.** 990 frames at 60 fps and 10 at 10 averages 57.1: a match that
  visibly stalls ten times in sixteen seconds, three frames off a perfect run. **Phase 3 has
  nothing open.**

**Do this alongside Phase 2, not at the end.** Every argument in phases 4 through 17 is settled
faster with a week of real numbers than a week of opinions, and this codebase already believes
that: `docs/VISION.md` § 5 is "verify by measuring" and there are sixty test probes to prove it.

Track: matches started and finished, leave rate by round, mode split, character and tsinelas pick
and win rates, match length distribution, queue times by rating band, crash and disconnect rate,
FPS distribution by hardware, settings actually used, and **where a first-time player stops**.

⚠️ **The first-launch funnel is the most valuable number in this document.** Launch, sign-in, menu,
first queue, first match started, first match FINISHED. It tells you what to fix before anything
else here is worth building, and it is about forty lines of code.

**The prompt for this phase is [§ 19.3](#193-prompt-for-phase-3).** Every prompt in
this file lives in § 19 so there is one place to copy from. § 0.5 is the standing preamble each
one inherits and § 0.6 is what to re-verify before trusting any of them.

---

## PHASE 4 · PROGRESSION: XP, LEVELS AND MASTERY ✅ SHIPPED 2026-08-30

⚠️⚠️ **`docs/TODO.md` § 91 IS THE AS-BUILT RECORD AND IT DEPARTS FROM THE TEXT BELOW IN
ONE PLACE THAT MATTERS.** This section and § 19.4 both say to detect an AFK seat using *"the
input telemetry the bots already produce"*, which reads as `InputIntent`. **The host never
receives a remote player's `InputIntent`**: `MatchRpc.SubmitMoveServerRpc` carries a transform,
not a key, so an intent-based check catches the local seat and the bots and nobody else. What
shipped reads MOVEMENT, which arrives for every seat and which `MatchStatsCollector` already
samples. § 91.1 has the derivation of the 5.06 m bar.

⚠️ **AND THE FLAT RATE IS FLAT IN BOTH DIRECTIONS NOW.** This section cuts diminishing returns,
rested XP and the daily cap by name. § 91.3 extends it to the LEVEL COST for the same reason: a
rising cost per level is diminishing returns wearing a different hat, felt identically by the
player and impossible for them to see the source of.

**The point:** give a player who just lost a reason to queue again. Rank goes down when you lose.
**Progress must never go down.** That asymmetry is the engine of every live game that works.

- Account XP from completion, placement, and a small set of per-match objectives. **Weight
  completion heavily and placement lightly**, so leaving is the only thing that costs.
- Account level, uncapped, with a new border every 50.
- ❌ **No season track. CUT 2026-08-31**, and the question that killed it is the right one to keep
  asking: *"remove seasonal rewards like dude what can we even give as rewards"*. A 50-tier track
  is 50 rewards to author every ten weeks, forever, by five students who also have exams, and
  **the first missed season makes the whole live-service framing collapse.**

### 4.1 ⚠️⚠️ WHAT A REWARD CAN ACTUALLY BE, WHICH IS THE REAL CONSTRAINT

His question was not rhetorical and it deserves a straight answer, because every progression idea
in these documents quietly assumed an art pipeline that does not exist. **Sort every reward by what
it costs to make, and only ever promise from the top of this list.**

| Reward | What it actually costs | Verdict |
|---|---|---|
| **A title** | A line of text in a data file. | ✅ **Free. This is the reward.** Hundreds are affordable. |
| **A banner badge** | One flat shape, one colour, no rig, no animation. | ✅ **Nearly free.** Dozens are affordable. |
| **A character colour variant** | Sixteen numbers. `ToonSkin`'s palette remap already does this per renderer, cached. | ✅ **Nearly free**, and it is the single most under-used asset in this codebase. |
| **A banner frame** | One 2D border. | ✅ Cheap, a handful per year. |
| **An emote or victory pose** | An animation. Somebody has to author it. | ⚠️ Expensive. One or two a year, not a track. |
| **A tsinelas or lata skin** | A new model, UV, texture and import pass. | ⚠️⚠️ **The most expensive thing on this list.** These are the props the whole game looks at, and there are already ten and six of them. Do not spend one on a progression tier. |
| **A new character** | Weeks. `docs/Voxel_Person_Log.md` records what ZACK actually cost. | ❌ Never a reward. |

⚠️ **So the honest progression is titles, badges and palettes, and that is genuinely enough.**
`INSPIRATION.md` § 2.5 is the argument: three chosen stat trackers and a title next to your name in
a lobby buys more status per hour of work than any model in the game. **Status is text and a
number. It has always been text and a number.**

⚠️ **AND IT MEANS ACCOUNT LEVEL AND CHARACTER MASTERY CARRY THE PROGRESSION ON THEIR OWN.** They
are permanent, they never reset, they need no seasonal content, and they pay out in exactly the
three currencies above. A season track was a fourth system delivering the same three things on a
deadline nobody set.

- Per-hero mastery, separately: play a hero, level that hero, earn its title and palette.
  ⚠️ **The six heroes only.** § 10 has the reasoning; the other twelve characters keep a played
  count and no path.
- ❌ **No soft currency and no shop. CUT 2026-08-31 on scope.** Rewards come straight off account
  level and character mastery. That deletes an economy, a shop screen, a price for every item
  forever, and duplicate protection, and a player loses nothing they can name.
- ⚠️ **Every reward on every track is cosmetic or expressive.** A player queuing ranked against
  someone 40 levels above them must be facing a better player, never a stronger account.

⚠️ **AN AFK PENALTY HAS TO EXIST BEFORE XP DOES.** The moment completion pays, standing still
pays. Reuse the input telemetry the bots already produce to detect a seat that has not acted for a
whole round, pay it nothing, and escalate on repeats.

⚠️⚠️ **THE XP RATE IS FLAT. NO DIMINISHING RETURNS, NO RESTED BONUS, NO DAILY CAP.** A match pays
what a match pays, forever, for everybody.

**Two versions of a rate curve were proposed and both were cut.** First diminishing returns, which
🧑 called correctly: *"players always notice and always complain"*, because it is a penalty for
playing your favourite game too much that arrives without warning mid-session. Then rested XP, the
same arithmetic framed as a bonus for returning, which he also cut: *"3 diminishing xp is doing too
much"*, *"dont do diminishing xp"*.

⚠️ **He is right and the second cut is the more instructive one.** Rested XP is a better mechanism
than diminishing returns, and it is still a whole extra system, with a pool to accumulate, a rate to
track, a UI to explain it and a rule nobody asked for, sitting on top of a progression that already
has an account level, a per-character mastery and a challenge engine.
**The problem it solves, that somebody who plays ten hours a day out-levels somebody who plays two,
is not actually a problem in this game**: nothing on any track affects a match (§ 0.5 rule 4), so a
higher level buys nothing but a border.

**A flat rate is one number, it is explainable in a sentence, and nobody has ever resented it.**

**The prompt for this phase is [§ 19.4](#194-prompt-for-phase-4).** Every prompt in
this file lives in § 19 so there is one place to copy from. § 0.5 is the standing preamble each
one inherits and § 0.6 is what to re-verify before trusting any of them.

---

## PHASE 4.5 · QUALITY CONTROL FOR PHASES 1 TO 4 ✅ SHIPPED 2026-08-30

⚠️⚠️ **`docs/TODO.md` § 94 IS THE AS-BUILT RECORD AND THE HEADLINE IS NOT IN THIS SECTION AT
ALL: NO CAREER HAD EVER REACHED THE SERVER.** Every match record this game has written since
Phase 2 was refused **422** and the upload queue had been permanently wedged behind the first one.
It was found the way § 4.5.1 asks for, by reading the running game's own log rather than this
plan, and it is exactly the fault the XP assertion below was commissioned to catch.
`ProfileRules` and `ProgressionRules` were correct throughout; the client stamped
`PlayerAccount.ConnectionToken` into a field the endpoint compares against `context.playerId`.
**§ 94.1.**

🧑 commissioned this on 2026-08-30, straight after the account UI rebuild: *"afterwards
continue on with phase 4 work then add a phase 4.5 quality control for phase 1-4"*.

**Why it earns a number of its own rather than being "tidy up as you go".** Phases 1 to 4 shipped
in two days and each one was verified against ITS OWN acceptance list. Nothing has ever been
verified across all four at once, and every fault found in them so far was found the same way: by
🧑 playing or looking, not by a test.

**The four that got through, and what each one proves about the gaps:**

| Found | How it was found | What that says |
|---|---|---|
| Cloud Code stripped every parameter, so `save`, `delete` and `submit` had **never once run** (§ 90.5) | A new probe called a DIFFERENT branch | Every probe tested the default branch. Coverage was one action out of four. |
| The account and career screens overlapped themselves and ran off the bottom of the screen (§ 92.1) | 🧑 photographed them | No UI probe covered any screen phases 1 and 2 built. |
| `ARewardCannotCarryAGameplayNumber`'s subject raced its own test suite (§ 91.5) | xUnit parallelism | A global that would have misreported XP in play about once in a hundred matches. |
| The AFK check as SPECIFIED could not work (§ 91.1) | Reading `MatchRpc` | A prompt written before the code it describes, followed literally, would have shipped a check that watched three seats out of four. |

⚠⚠ **NONE OF THOSE ARE BUGS IN A FEATURE. They are gaps in what was being asked.** That is what
this phase is: ask the questions nobody asked the first time.

### 4.5.1 Re-verify every acceptance bullet against the CODE

§§ 1, 2, 3 and 4 each end with a "done looks like" list. **Read each bullet, then prove it from
the running game rather than from `docs/TODO.md`.** § 0.5 rule 2: where a document and the code
disagree, the code is right. Fix the document in the same commit.

⚠️ **CORRECTED 2026-08-30: ONLY §§ 1 AND 2 HAVE ONE.** Phases 3 and 4 end in bullets and no
"done looks like" sentence was ever written for either, so the bullets are the acceptance list.
`docs/TODO.md` § 94.3 walks all four that way.

The ones most likely to have rotted, because nothing exercises them:

- **A fresh install reaches the menu signed in with no prompt** (§ 1). Nothing tests a FIRST
  launch. Delete the local profile and the UGS cache and boot.
- **An account can be deleted** (§ 1). `player-account`'s `delete` branch had never run before
  § 90.5 and has still never been run by a probe.
- **Career totals survive a reinstall on the same account** (§ 2). Never tested end to end.
- **A match awards XP computed server-side** (§ 4). `UgsServicesProbe` proves the endpoint
  ACCEPTS a record; nothing yet reads the profile back and asserts the XP moved by the amount
  `ProgressionRules.MatchXp` says it should. **That is the single most valuable check in this
  phase**, because it is the one that would catch the C# and the JS drifting apart in the only
  place a player would notice.

### 4.5.2 Cover every branch of every endpoint, not the default one

⚠⚠ **§ 90.5 IS THE TEMPLATE FOR THIS WHOLE PHASE.** Three scripts, four actions each, and a
missing parameter produced a well-formed answer from the wrong branch with nothing logged. Write
a probe case per ACTION, and make each assertion a string only that branch can produce.

`player-account`: `load`, `save`, `delete`, **`attest`**, `verify`. `match-record`: `load`,
`history`, `submit`. `telemetry`: `submit`, `report`. **Count them: TEN.**

⚠️⚠️ **CORRECTED 2026-08-30: THIS PARAGRAPH SAID NINE AND OMITTED `attest`, WHICH WAS THE ONLY
BRANCH IN ANY OF THE THREE SCRIPTS WITH NO COVERAGE IN EITHER DIRECTION.** It is the half of the
impersonation guard that MINTS a proof.
`UgsServicesProbe.TheAccountEndpointRefusesAHandleProofItNeverMinted` covers the refusal and says
in its own header that it can only ever cover the refusal, because a probe player has never saved
a profile and so attest has no handle to vouch for. **A phase written to find the branch nobody
tested listed the branches from memory and missed one.** `docs/TODO.md` § 94.2 is the as-built
table; `CloudEndpointActionProbe` is the file.

### 4.5.3 A UI probe for every screen phases 1 to 4 built

`PlayerHubLayoutProbe` covers the hub, the sign-in screen and the nameplate as of § 92.5. Not
covered: **the end-of-match XP block** (`MatchResult`), and the telemetry row in the settings
panel. Both are phase surfaces and neither is measured.

⚠️ **AND THE PROBE ONLY ASSERTS FIT AND LEGIBILITY.** It cannot see a screen that is ugly, and
it cannot see a control nobody can find. It caught a type scale below the readable floor on its
first run, which is exactly the class it is for; do not read a green run as "the UI is good".

### 4.5.4 Offline and LAN, per § 0.5 rule 7, for all four phases at once

Each phase degrades correctly on its own. **Nobody has pulled the cable and walked the whole
thing:** boot, menu, hub, career, a LAN match, the end-of-match XP bar, and the queue flushing on
the next sign-in. § 90.4 lists what is automated in place of the four-machine run and that list
now needs the Phase 4 rows added to it.

### 4.5.5 The deferred list, written down once

Things known to be absent, so nobody rediscovers them as bugs: the avatar (§ 1.4, an open
argument), the rank badge (§ 9), achievements (§ 10), compare with a friend (§ 6), the rename
cooldown and profanity filter (§ 1.3, written and never built), email recovery (§ 1.2), and
**every Phase 4 reward, which is computed and worn by nothing** (`docs/TODO.md` § 91.8).

### 4.5.6 What this phase must NOT turn into

⚠⚠ **IT IS NOT A REDESIGN AND IT IS NOT A BALANCE PASS.** Every number in Phase 4 is an
unmeasured starting point and § 91 says so of each one; moving them belongs to whoever has a week
of telemetry, not to a QC pass. **Find what is broken or unproven, prove it or fix it, write down
what is deliberately absent, and stop.**

**Done when** every acceptance bullet in §§ 1 to 4 has a named test or a named reason it cannot
have one, every endpoint action has a probe case asserting a string only that branch produces, the
XP a real submission awards is asserted against `ProgressionRules.MatchXp`, and § 0.5 rule 9 is
satisfied.

**The prompt for this phase is [§ 19.4b](#194b-prompt-for-phase-45).**

---

## PHASE 5 · COSMETICS AND CHARACTER CUSTOMISATION ⚠️ REWORKED TWICE, 2026-08-31

⚠️⚠️ **ROSTER INTEGRITY VS. THE DEDICATED "CREATE YOUR OWN CHARACTER" SYSTEM.**
`docs/TODO.md` § 107. A previous pass misunderstood 🧑's vision and applied a whole-body hue/tint slider across the entire roster, resulting in classic characters like **Berto** turning alien cyan and magenta with illegible skin tones. 🧑 corrected this immediately:
> *"i didnnt want all characters to be customizable. I just wanted tehre to be a create ur own charcter slot and u can fully customize it (facial expression, clothes, skinn tone, height, size, accessories, everythinngs ), theres like 3 characters u can save at once but only onne is used. i didnt want it to be appliable to all characters wth. maybe the heroes we can change their clothes and shit but donnt touch the skin and shit of classic wtf"*

### 5.1 ⚠️⚠️ THE SECOND REWORK, AND WHY THE FIRST ONE ANSWERED THE WRONG HALF

**`docs/TODO.md` § 108.** The pass that read the quote above deleted the TINT and STRENGTH rows from
character select outright and replaced them with a button. That got three things wrong at once, and
the third is the one worth remembering:

1. **It threw away the half he asked to keep.** The same sentence says *"maybe the heroes we can
   change their clothes and shit"*. Clothes are what the dial mostly turns; skin was the only part
   he objected to.
2. **It could not undo the damage it was written for.** `ConvertedCharacterSelect.ShowModel`,
   `MatchInstaller` and `MatchRpc` all still applied `SettingsStore.LookFor`, so **every hue already
   saved to disk was still being painted with the only screen that could reset it now deleted.** A
   player whose Berto was green stayed green.
3. **The button it left behind went nowhere.** It called
   `FindFirstObjectByType<CustomCharacterCreator>()` and nothing in the project ever created one.

**The fix is one slot list, not one screen.** `PaletteRules.IsProtectedSlot` now holds the three
SKIN slots (13, 14, 15) beside the FACE slot (8) it already held, so a recolour physically cannot
reach anybody's skin, on either side of the wire, and the clothes stay free. Twelve of the sixteen
slots remain reachable and `RosterIntegrityTests` asserts that number, because a rule that protected
everything would be indistinguishable from having no dial at all.

### 5.2 The two distinct character categories

1. **EVERY NAMED CHARACTER: the twelve Classic street kids and the six heroes (DANTE, CHESKA, SEAN,
   ZACK, NEMU, PHAISTER)**
   - **Skin and face are locked**, by `PaletteRules.IsProtectedSlot` rather than by a convention.
   - **Clothes are free from level one.** The CLOTHES and STRENGTH rows on character select, twelve
     hue swatches and three saturation steps, not gated on anything.
   - **Earned palettes stay earned** and are the named presets in the COLOURS row above them.
   - ⚠️ **`Berto` IS NOT A HERO.** This list read *"heroes (Berto, Sean, Dante, Cheska,
     Zack, Nemu, Phaister)"* in five documents at once, and `HeroLoadoutRules` then shipped ability
     sidegrades for him while omitting Phaister. `Roster.HeroPeople` is the list; `bayan`, display
     name BERTO, is the first of the twelve Classic characters and has no kit.

2. **MAKE YOUR OWN: the one character whose everything is a dial**
   - **The door is a row on character select**, at the bottom of the same list you pick a character
     from, reading `MAKE YOUR OWN  ·  SLOT n: <name>`. ⚠️ **One door, and it is where you
     already are.** `CLAUDE.md` § 6.3: every destination has a visible door, and never add a second
     door to fix a findability problem.
   - **Three save slots, one active.** `CustomCharacterRules.MaxSlots`, persisted as three wire
     strings in `GameSettings.CustomCharacterWires`.
   - **The screen is `CustomCharacterScreen`**: the model on the left at full size, six sections on
     the right, and a `< NAME  n/total >` stepper per choice. The camera moves to the section
     (`ModelPreview.LookAt`), and BACK discards while KEEP AND USE writes.
   - **What is actually customisable today**

     | Section | Rows | Count |
     |---|---|---|
     | Face | skin tone, expression, marks | 32, 18, 14 |
     | Hair | cut, colour | 18, 24 |
     | Body | height, build | 7 steps of 5 per cent, 3 |
     | Clothes | top, top colour, bottom, bottom colour | 16, 16, 12, 16 |
     | Gear | headwear, eyewear, wrists, neck | 18, 14, 10, 12 |
     | Kit | footwear, tsinelas, lata, borrowed hero kit | 10, 6, 6, 6 |

   - ⚠️⚠️ **EVERY ENTRY IS GEOMETRY, AND `CustomCharacterWardrobeTests` FAILS IF ONE IS NOT.**
     `docs/TODO.md` § 110. `VoxelWardrobe` authors the boxes and `VoxelDresser` hangs them on the
     MEASURED head, torso, arm and legs, so one authored hat fits a cast that spans 132 mm. The
     lists got SHORTER doing it: 48 hairstyles became 12, 48 tops became 10, 32 hats became 12.
     **Twelve hats that exist beat thirty-two names that do not**, and the number a player feels is
     the combination count, which is over four billion looks before wrists, neck, footwear, height
     and build are counted.
   - ⚠️⚠️ **AND THE RIG UNDER IT IS NAKED, WHICH IS THE PIECE THIS PHASE WAS MISSING.**
     `docs/TODO.md` § 112. `CustomCharacterRules.BaseRigId` resolves `team-custom-base.glb`
     (`tools/build_base_voxel.py`): bald, bare, no face. Against the dressed rig it replaced,
     **every wearable had to COVER what was under it rather than BE the thing** — a hairstyle was
     a lid over a baked mop, a sando was a box over another box, and each of the twelve expressions
     laid a skin-coloured plate over the rig's own painted eyes first. That is the same three
     pieces every game with a character creator is built from: **a naked base mesh, per-slot
     equipment geometry, and a colour remap**, and this phase had the second and the third.
     ⚠️ **Nothing existing was repointed**: `custom` and `team-custom.glb` are untouched and
     `custom_base` is a new row.
   - ✅ **AND IT ENTERS A MATCH.** `LobbySeatInfo.Custom` carries the `C3` frame,
     `MatchInstaller` spawns the base rig and dresses it, and `HeroAbilitySystem` is given
     `KitFor(HeroKitId)`. ⚠️ `NetSession.ProtocolVersion` is **19**; both machines rebuild.
     ⚠️ **`custom` is still not a row in `Roster.AllPeople`** and must not become one.
   - ⚠️⚠️ **THE CUSTOM CHARACTER BORROWS ONE HERO'S KIT, WHOLE, AND CANNOT MIX.** 🧑,
     2026-08-31: *"it can js borrow the skills of any of the characters for its skills and ult"*,
     then *"it can only follow onne skill tree tho and cant mix diff shits"*. `HeroKitId` is ONE
     string, so a mixture is not something a modified client can send; a custom character telegraphs
     exactly like the hero whose kit it carries, which is what keeps `docs/VISION.md` § 4's
     ability tells true. § 110.5.

**Slots:** Custom character creator (3 save slots), hero outfits/clothes, headwear, tsinelas skin, can skin, emote wheel, victory pose, and **the banner**.

⚠️⚠️ **THE BANNER IS ONE OBJECT AND IT ABSORBS EVERY OTHER IDENTITY SURFACE. CUT 2026-08-31.** An
earlier version of this list had a nameplate, a title, a badge, an emblem, a frame, a border, a
mastery number and an avatar as **separate** cosmetic slots, each with its own inventory category,
its own UI row and its own wire field. They all do the same job: they say who you are next to your
name.

**So there is one banner**, carrying a frame, a pose, a badge, a title and three chosen stat
trackers (`INSPIRATION.md` § 2.5), and it is what appears in the lobby, on the scoreboard, on the
profile and at the end of a match. One object to author, one to replicate, one to earn things for.
**Everything that used to be its own slot is now a field on the banner.**

**Sources, all free:** account level, hero mastery, ranked season rewards, achievements.
⚠️ **Weekly challenges were a source here and are cut**, § 13. ❌ **No currency and no shop**, cut on 2026-08-31: § 4. No lootboxes, no gacha, no real money.

**What makes it cheap here:** `RosterBook` and `RosterEntryAsset` resolve id to model,
palette and clips, `ToonSkin`'s palette remap recolours a whole character from 16 slots per
renderer, and `docs/wearables_catalog.md` defines the wearable contract.

⚠️ **THIS PARAGRAPH USED TO END *"a colour variant of any character is nearly free
today"* AND THAT WAS DELETED WITHOUT A REASON.** It is still true and it is still why this phase is
affordable; what changed is that four of the sixteen slots are now out of reach
(`PaletteRules.IsProtectedSlot`), so a variant recolours the clothes and never the person.
`CLAUDE.md` § 3: record the deletion and the reasoning, not just the change.

**One extra that is worth more than it costs, and it is BUILT:** a **favourite loadout per
character**, so switching character does not mean re-dressing. `CharacterLoadout` is that row and
`GameSettings.CharacterLoadouts` is the list. ⚠️ **This bullet was deleted on 2026-08-31 as
though it were unbuilt work being cut**, and it had shipped in § 101.

⚠️⚠️ **GIVE COSMETICS STRING IDS, NOT WIRE INDICES.** Every cosmetic id is something another peer
resolves. `Roster.Slippers` records at length what inserting a row into a wire-facing list does.
Pay the few extra bytes: it removes the entire class of bug permanently and this is the last cheap
moment to decide it.

⚠️ **A COSMETIC MUST NEVER CHANGE A SILHOUETTE ENOUGH TO CHANGE A READ.** This is a game about
seeing which of three attackers is committing. Headwear that doubles a character's height is a
competitive change wearing a cosmetic label. Bound the volume and write the bound down.

⚠️ **Preview through `ModelPreview` with the real shader, never a flat icon.** This project already
learned that a render from one camera is not evidence about another.

**The prompt for this phase is [§ 19.5](#195-prompt-for-phase-5).** Every prompt in
this file lives in § 19 so there is one place to copy from. § 0.5 is the standing preamble each
one inherits and § 0.6 is what to re-verify before trusting any of them.

---

## PHASE 6 · SOCIAL

✅ **SHIPPED 2026-08-31, WITH THREE EXCEPTIONS NAMED BELOW. `docs/TODO.md` § 102 is the as-built
record.** The rules are `Core/Social.cs`, the endpoint is `ugs/cloud-code/social.js` and it is
deployed, the screen is a FRIENDS tab on the hub, and the invite is an ADD button per human on the
end-of-match board.

⚠️⚠️ **TWO OF THE BULLETS BELOW DEPEND ON A MATCHMAKER THAT DOES NOT EXIST UNTIL PHASE 7, AND ONE
DEPENDS ON A SERVICE QUERY CLOUD SAVE DOES NOT HAVE.** They are marked, and § 102.2 has the
reasoning in full rather than a shrug:

- ✅ **CLOSED 2026-08-31 BY PHASE 7, EXACTLY AS THE NEXT SENTENCE PREDICTED.** `PartyRules` is the
  rule set and `QueueCard` presses it: QUICK MATCH in a room of three looks for a lobby with three
  chairs. **The rail did not change.** `docs/TODO.md` § 106.3.
- ⚠️ **Parties that "queue together" cannot be built before there is a queue.** What ships is the
  thing that queue would replace: a friend in a joinable lobby publishes its join code with their
  presence, and JOIN on the rail hands it to `LobbyJoinPanel`. When Phase 7 lands, a party becomes
  a queue ticket and the rail does not change.
- ⚠️ **"By display name and tag" needs an index document.** Cloud Save is keyed by player id with
  no query-by-value, so resolving `Maria Clara#4417` means a second document every rename has to
  update, with a real failure mode (a stale index hands a request to the wrong account). **Recent
  players ships instead**, which this section itself calls the highest-converting path.
- ✅ **AND BLOCKING NOW SURVIVES A REAL QUEUE, AT TWO GATES.** `MatchmakingRules.Evaluate` refuses
  a blocked host BEFORE the rating is even considered, and connection approval still refuses one
  that gets through. Approval alone would let the queue find a blocked host, connect, and bounce
  the player straight back out, which reads as the queue being broken rather than as a block
  working.
- ⚠️ **Blocking survives the only matchmaking there is**: a blocked account is refused at
  connection approval, so it cannot join a lobby you host. Phase 9 inherits the rule.

⚠️ **AND NO TWO-ACCOUNT RUN HAS HAPPENED.** § 102.5: the probe has one throwaway UGS profile, so
`request`, `accept`, `decline` and `remove` are proven by the core tests and by reading the
deployed script, not by a live round trip between two players. That is a thirty-minute manual pass
on the two laptops and it is the next thing to do with this feature.

- Friends by id, by display name and tag, and by a share code.
- Presence: online, in menu, in queue, in a match, spectating.
- **Parties**, which is the real retention driver, because a game people come back to is a game
  their friends are in. A party of 2, 3 or 4 queues together.
- Invites from the friends list and **from the end-of-match screen**, which is the
  highest-converting social prompt any game of this shape has.
- Recent players, with the match they were in, and a one-click add.
- Blocking, which must survive matchmaking: a blocked player is never queued into your match.
- Clubs later, and only if friends and parties are being used.

⚠️ **A PARTY OF FOUR IS A FULL MATCH, WHICH IS A RANKED PROBLEM.** Four friends can arrange
results between themselves. Either exclude full parties from ranked or accept only partial ones.
Decide it in Phase 9 and assert it in a test.

✅ **DECIDED 2026-08-31: A FOUR-STACK CANNOT QUEUE RANKED AND PARTIES OF TWO OR THREE CAN.**
`PartyRules.MaxRankedSize`, and `PartyTests.AFourStackCannotQueueRankedAndATwoOrThreeStackCan` is
the test § 19.9 step 9 asked for. `docs/TODO.md` § 105.4 has the reasoning: excluding EVERY party
would be a shorter rule and a worse game, because two friends cannot arrange a four-player result
between themselves. The other two seats are strangers who are trying to win.

⚠️ **Extend `LobbyChat`.** It carries hard-won layout notes and there must never be a second one.

**The prompt for this phase is [§ 19.6](#196-prompt-for-phase-6).** Every prompt in
this file lives in § 19 so there is one place to copy from. § 0.5 is the standing preamble each
one inherits and § 0.6 is what to re-verify before trusting any of them.

---

## PHASE 7 · MATCHMAKING ✅ SHIPPED 2026-08-31

**`docs/TODO.md` § 103 is the as-built record.** `Core/Matchmaking.cs` is the arithmetic,
`Net/Matchmaker.cs` is the driver, `UI/QueueCard.cs` is the surface, and it adds **zero** service
requests: it subscribes to the browse loop `ServerQuery` already runs every 4 seconds.
⚠️  **What is still open from `INSPIRATION.md` PROMPT I5 is the population GATE**, and it is open
because with one queue there is nothing to gate. § 103.6.

- **Tier 1, free and available today: a Lobby-backed queue.** The game already browses UGS Lobby
  every 4 s and resolves join codes. QUICK MATCH queries for a joinable lobby whose advertised
  rating band contains the player, joins it, and hosts one advertising its own band if none is
  found. That is skill-based matchmaking with no matchmaker service and no cost.
- **Tier 2, later: UGS Matchmaker** with real tickets and pools. The queue UI does not change.
- Band widening so a queue never dead-ends: start at plus or minus 100 rating, widen by 100 every
  15 s, stop at plus or minus 500 and take anybody. **Show the widening**, so a long queue reads as
  progress rather than as a hang.
- Backfill: a match that loses a player advertises the seat rather than dying.
- Region from Relay's own list. **Manila to Singapore measured 48 ms**, which is the number this
  game is tuned against.
- **Separate pools by input device and by platform** (Phases 14 and 15), which is free and removes
  the entire aim-assist argument before it starts.

⚠️⚠️ **A 4-PLAYER FREE FOR ALL MATCHES DIFFERENTLY FROM A TEAM GAME, AND THIS IS THE PART TO GET
RIGHT.** There is no team to balance. The job is not "make two sides equal", it is "put four
players of similar skill in one room". **The quality metric is the SPREAD of the four ratings**,
not the gap between two averages. A lobby with one 1400 and three 900s is a bad match even though
every team-based fairness formula calls it balanced.

⚠️ **THE TAYA ROTATION IS WHAT MAKES THIS FAIR AT ALL**, and it is worth saying in the queue UI:
everyone defends once, so a bad first round is not a lost match.

**The prompt for this phase is [§ 19.7](#197-prompt-for-phase-7).** Every prompt in
this file lives in § 19 so there is one place to copy from. § 0.5 is the standing preamble each
one inherits and § 0.6 is what to re-verify before trusting any of them.

---

## PHASE 8 · COMPETITIVE INTEGRITY ✅ SHIPPED 2026-08-31

**`docs/TODO.md` § 104 is the as-built record, and § 104.1 is a correction to § 8.1 below that
changes the whole mechanism.**

⚠️⚠️ **PHASE 9 DEPENDS ON THIS. IT IS THE PHASE THAT DECIDES WHETHER ANY RANK MEANS ANYTHING.**

### 8.1 A witnessed result, because the host is a player

⚠️⚠️ **THE PARAGRAPH BELOW THAT SAYS "THE CLIENTS ALREADY HAVE EVERYTHING NEEDED" WAS WRONG,
AND IT WAS THE LOAD-BEARING CLAIM.** Corrected 2026-08-31, `docs/TODO.md` § 104.1. Peers receive
every scoring event and **derived nothing from them**: at the whistle `MatchRpc.BroadcastMatchRecord`
sends the host's finished `MatchRecord` and every peer calls `GameServices.Stats.Adopt` on it, so
four "independent" submissions were four byte-identical copies of one machine's opinion. Comparing
them would have proved that JSON round-trips. **What shipped instead compares against the EVENT
STREAM**: `Net/ScoreWitness.cs` tallies `MatchDirector.Scored` on every peer and hashes its OWN
scores into the host's record.

⚠️  **That also removes the hardest question this section left open, which is who chooses the
witness.** A witness chosen by the host is a witness chosen by the suspect. With every peer already
submitting, nobody chooses.

⚠️  **And the honest limit is stronger than the one this section states.** It is not only two
colluding players: **a modified host that awards itself points DURING the match is not caught at
all**, because every peer tallies the same fabricated events. § 8.2's dedicated servers are the
answer to that one. `docs/TODO.md` § 104.3 has the full list of what this does and does not stop.

`MatchDirector.AddScore` runs host-side and the host is one of the four. A modified client that
hosts can award itself anything.

**The answer is a witness.** The host submits the scoreboard, and **one peer, chosen at random at
match end, submits its own independently derived copy.** The endpoint accepts the result when the
two agree and flags the match when they do not. Two submissions per match, not four.

⚠️ **The clients already have everything needed.** Every peer derives the scoreboard from the
scoring events it already receives, because that is how the HUD stays in sync. Nothing new has to
cross the wire.

⚠️⚠️ **THIS WAS FOUR-PEER UNANIMOUS CORROBORATION AND IT WAS SIMPLIFIED ON 2026-08-31.** Requiring
all four to agree meant four submissions, four derivations to keep in step, and a disagreement
mechanism with four possible minorities to reason about. **A random witness catches the same
cheater**: a lying host does not know which peer will be asked, so it has to produce a scoreboard
that survives an honest check either way, which is exactly the bar unanimity set. Half the traffic
and half the code for the same guarantee.

⚠️ **What it does not stop**, and this is unchanged: two colluding players, one hosting and one
witnessing. Neither did the four-peer version stop four colluding players. Write the limit down in
`docs/TODO.md` rather than implying a stronger claim.

### 8.2 The real answer, for the day there is a budget

Dedicated servers through Multiplay, with the host role off a player entirely. Everything above is
arranged so this slots in behind the same endpoint without touching ranked, progression or profile
code.

### 8.3 The rest

- Reporting from the end-of-match screen and the profile, with a reason.
- **Leaver penalties that distinguish a leave from a disconnect** using the reconnect window
  `LobbySession` already implements, or a player with bad internet is punished for their ISP.
- Escalating queue cooldowns, and rank loss for a ranked leave.
- Rate limits on every write, because a free tier is a budget an abusive client can spend.
- Sanity checks on submitted records: impossible scores, impossible durations, impossible rates.
- **Smurf handling, and it needs no system at all:** a new account with a very high early win rate
  gets a wide rating deviation and climbs fast. Glicko-2 does this for free if the deviation is not
  clamped too tightly, which is a real argument for it over plain Elo.
- ❌ **No trust score and no behaviour-sorted pools. CUT 2026-08-31.** `INSPIRATION.md` § 2.8 has
  the reasoning: a trust score exists to sort players into pools, and this population cannot fill
  the pools it already has. Reporting and the avoid list do the job.

**The prompt for this phase is [§ 19.8](#198-prompt-for-phase-8).** Every prompt in
this file lives in § 19 so there is one place to copy from. § 0.5 is the standing preamble each
one inherits and § 0.6 is what to re-verify before trusting any of them.

---

## PHASE 9 · RANKED, RATING AND SEASONS ✅ SHIPPED 2026-08-31

**`docs/TODO.md` § 105 is the as-built record.** The three questions this section says to ask
were asked on 2026-08-31 and answered: **the ladder is on HERO STRIKE**, there are **five tiers with
no divisions**, and the names are **BATA, KANTO, BARANGAY, KAMPEON, ALAMAT**.
⚠️  **`match-record.js` knowingly approximates `RatingRules.UpdateAll` in one way and § 105.5
says exactly how and what it costs**: the endpoint is called once per player and cannot read three
other rank documents on a free tier, so it treats every opponent as sitting at the start rating.
The ORDER of the ladder is unaffected. The fix for the day there is a budget is written down.

⚠️⚠️ **DO NOT BUILD THIS BEFORE PHASE 8.1.** A rank a host can award themselves is worse than no
rank, because it turns every good player's win into an accusation.

- ❌ **NO SECOND LADDER. ONE RANK. CUT 2026-08-31.** Classic and Hero Strike were to get separate
  ratings, on the reasoning that they are separate games and a player who is 1600 in one may be 900
  in the other. That reasoning is correct and it is still the wrong trade, because it leaves a
  player with **two ranks and no answer to "what rank are you"**, which is the only question a
  ladder exists to answer. **One competitive ladder; the other mode is unranked.** 🧑 chose this on
  player-facing complexity rather than on cost.
  ⚠️ **Which mode carries the ladder is not decided here.** Hero Strike is the higher-ceiling mode
  and `VISION.md` § 1 says it exists to raise the competitive ceiling, so it is the obvious
  candidate, but 🧑 has not said so. Ask before building.
- **Glicko-2, adapted for a 4-player free for all.** ⚠️ **THIS IS THE ARITHMETIC, NOT A FEATURE
  THE PLAYER MEETS.** It is Elo with one addition: it also tracks how confident it is about you, so
  a new player's rank moves fast and a settled player's moves slowly. The player never sees the
  number, only the tier, so it costs nothing in player-facing complexity and it is what stops a new
  account needing fifty games to land near the right tier. Elo is a two-player system and this is
  not a two-player game. Resolve a result as **six pairwise outcomes** (1st beat 2nd, 1st beat 3rd, and
  so on), feed all six in, and scale the step so one match moves a settled player about as much as
  one game should. Glicko-2's rating deviation is what makes a new player converge in ten games
  instead of a hundred, and it is what makes smurf handling free.
- **Visible tiers over the hidden number**, named in the game's own voice rather than
  Bronze-to-Diamond. Suggested shape, five tiers of three divisions plus a numbered apex:
  **BATA, KANTO, BARANGAY, KAMPEON, ALAMAT**, with the apex a live leaderboard. 🧑 names them.
- ❌ **No placement matches. CUT 2026-08-31.** Five games in a hidden state with their own rules
  and their own UI was a separate concept doing a job Glicko-2 already does by itself. **Start
  everyone mid-ladder with a wide rating deviation and show the tier immediately.** It converges in
  the same handful of games, and a new player sees where they stand on their first match instead of
  their sixth.
- **Seasons:** ten weeks. Soft reset toward the mean, never a wipe. Keep a permanent peak on the
  profile, because the peak is the thing people brag about.
- **No decay.** Decay punishes people with jobs and school, which is this whole audience. If the
  apex ever needs it, apply it only there.
- **RANK FLOORS.** Once a tier is reached the season cannot fall below it. `INSPIRATION.md` § 2.19.
- ❌ **No demotion buffer and no score-margin multiplier. CUT 2026-08-31.** Ranked had six
  sub-systems and two of them were paying for very little. The **margin multiplier**, worth up to
  1.25x for a stomp, is a tuning surface that has to be balanced forever in exchange for a nuance
  nobody will feel. The **demotion buffer**, needing two losses at a tier floor rather than one,
  solves exactly the feeling rank floors already solve, so keeping both is paying twice for one
  fix. What remains is Glicko pairwise, the soft reset and the floors.
- **Rewards: a title and a banner badge, and nothing else.** § 4.1 sorts every possible reward by
  what it costs to author, and a title is a line of text while a tsinelas skin is a model, a UV, a
  texture and an import pass. ❌ **The season border and the exclusive tsinelas skin are cut.**
  A tier title on your banner is the reward, and it is the one people actually screenshot.

**The prompt for this phase is [§ 19.9](#199-prompt-for-phase-9).** Every prompt in
this file lives in § 19 so there is one place to copy from. § 0.5 is the standing preamble each
one inherits and § 0.6 is what to re-verify before trusting any of them.

---

## PHASE 10 · LOADOUTS, SKILL VARIANTS AND ACHIEVEMENTS ✅ SHIPPED 2026-08-31

⚠️⚠️ **THIS PHASE IS BUILT AND ANYTHING IN THIS FILE CALLING IT UNSCHEDULED IS STALE.**
`Core/HeroLoadout.cs` holds the ability variants (budget neutral, asserted by
`HeroLoadoutTests`), `Core/Achievements.cs` holds the shelf, and both have rows on `PlayerHub`'s
CAREER tab. `docs/TODO.md` § 108 is the as-built record.

⚠️⚠️ **AND BOTH WERE UNREACHABLE ON A FRESH ACCOUNT UNTIL 2026-09-01, WHICH IS WHY "SHIPPED" AND
"REACHABLE" GET ASKED SEPARATELY.** `BuildCareerTab` bailed to `EmptyCareer` at zero matches and
these two groups are built after that line, and `PlayerHub._mode` defaulted to Classic while the
game defaults to Hero Strike, so the builds group was hidden behind a mode the player had not
chosen. `docs/TODO.md` § 114.12.

⚠️⚠️ **AND "SHIPPED" HAD A THIRD MEANING THIS PAGE WAS NOT ASKING FOR, WHICH IS WHETHER ANY OF IT
CHANGED A MATCH. IT DID NOT UNTIL 2026-09-01.** Until then the system stored a selection, printed
a percentage beside it and was read by nothing: `HeroLoadoutRules.ChallengesEnforced` was `false`,
so all twelve alternates were handed out and every challenge string described something the
player already had, and no kit consulted the equipped variant at any point. **`docs/TODO.md`
§ 114.16 is the as-built record of making it real** and the four bullets below now describe
shipped behaviour rather than intent:

- **The challenge is a local, Practice-safe count of successful casts** (`AbilityChallengeProgress`
  in `settings.json`, incremented on the local owner only). ⚠️ It is deliberately NOT the reward
  ledger every cosmetic unlock reads: a reward is written by `match-record.js` off a submitted
  career, and Practice submits nothing, so reading it would have made the Practice promise below
  false with nothing logging it.
- **All twelve alternates change the match**, through `AbilityContext.GainScale`/`CostScale` and
  `HeroAbilitySystem.VariantGain`/`VariantCost`. ⚠️ **Every telegraph and reticle number is read
  off the same table row the effect uses**, so the ring the player aims with cannot drift from the
  blast it promises.
- **The build is public and it is on the wire**: `LobbySeatInfo.Build` carries a `B1` frame, the
  host re-encodes what it decodes, and it is drawn on the lobby identity strip and the result
  board. ⚠️ `NetSession.ProtocolVersion` is **20** for it.
- **Sidegrade, still asserted**, and two rows were rewritten in the same session for failing the
  other half of that bar: 🧑, *"dont watn them to read as useless or the exact same"*. Arc Line was
  buying four frames of extra stagger and Short Leash was selling a leash radius that does not
  exist. **Budget neutrality is what stops an alternate being stronger; it does nothing to stop
  one being pointless, and only a person can tell you which.**

🧑: *"you know how overwatch or drg has unlockable skill paths or some shit?"*, then, clarifying
what he actually meant: *"like skill / ability / passive variations, for example in drg theres
diff guns"*, and how they are earned: *"u get them like in cod u finish quests or like ror2"*.

**That is a loadout system with challenge-gated unlocks, and it is a better feature than the
"skill tree" reading it replaced.** The full design, the model it borrows from, the swap-at-role-
change idea and the reasoning live in [`INSPIRATION.md`](INSPIRATION.md) § 5. The short version:

- Each hero gets **a small pool of options per slot** (skill 1, skill 2, ultimate, passive), not a
  ladder of upgrades. You pick a build before the match, the way you pick a primary in Deep Rock.
- **Every option is a sidegrade at the same ability budget.** Nothing unlocks more damage, range,
  duration or a shorter cooldown. A test asserts it.
- **Unlocks are Risk of Rain 2 style challenges**, character-specific and characterful, so earning
  the variant teaches the character. Not a level wall.
- ⚠️⚠️ **AND EVERY CHALLENGE MUST BE COMPLETABLE IN PRACTICE AGAINST BOTS.** That single rule is
  what makes the whole system safe in a competitive game: the gate costs time spent learning a
  character, never matches won against people, so nobody has to grind ranked to be equipped for
  ranked. `INSPIRATION.md` § 5.4 has the argument in full.
- **The build is public.** Shown in the lobby and on the scoreboard, because hidden loadouts in a
  four-player fight are information asymmetry that feels like cheating.
- **Achievements** are the same machinery pointed at bragging rather than at unlocks:
  `INSPIRATION.md` § 5.6.

**Then the part that is pure upside and has no balance risk at all:** per-hero mastery paths
of things that change nothing. A signature victory pose, a character emote, a voice line set, a
nameplate, that character's own tsinelas, a colour variant, a title, a visible mastery number.
**Most of the grind should live here.** It is Phase 5 content wearing a Phase 10 structure.

⚠️⚠️ **MASTERY PATHS ARE FOR THE SIX HEROES ONLY, NOT ALL EIGHTEEN CHARACTERS. NARROWED
2026-08-31.** 🧑, choosing on what the player has to hold in their head: *"for mastery paths only
give it to the heroes (6)"*. Eighteen paths is eighteen parallel grinds on one profile, and the
twelve non-hero characters have no kit to learn, so a path behind them is a grind attached to
nothing. **A hero has skills, an identity and a reason to be studied; that is what a mastery path
is for.** The other twelve keep a visible played-count and nothing else.
⚠️ **This does not shrink Phase 5.** The cosmetics themselves still apply to any character. What
narrows is the number of PATHS the player is asked to track, which is the whole point.

❌ **STREET HYPE IS NOT A SECOND PROGRESSION TRACK. CUT 2026-08-31.** Classic was to get its own
path, extending Street Hype with titles, a curve, bank recognitions and streak records. That is a
parallel progression system whose only reason to exist is which mode you happened to pick, so the
same match feeds a different bar depending on a lobby toggle, and the profile grows a second
level number nobody can explain. **Street Hype stays exactly what it already is: an in-match feel
in Classic.** It earns account XP like everything else and it has no track behind it.
⚠️ **`VISION.md` § 1's rule is untouched: Classic never gets powers.** That rule was never about
Classic needing its own progression, and this cut does not weaken it.

**The prompt for this phase is [§ 19.10](#1910-prompt-for-phase-10).** Every prompt in
this file lives in § 19 so there is one place to copy from. § 0.5 is the standing preamble each
one inherits and § 0.6 is what to re-verify before trusting any of them.

---

## PHASE 11 · BOTS, BACKFILL AND THE POPULATION PROBLEM

**This phase is not glamorous and it is the difference between a game that lives and one that
does not.** A 4-player game with 30 concurrent players has a queue problem that no amount of
ranked polish fixes, and the fastest way to make a competitive game feel dead is an empty queue.

- ✅ **Difficulty tiers for bots: ALREADY BUILT, verified 2026-09-03 against the code.** This is
  § 0.6 earning its keep: the bullet read as unstarted work and the whole thing is shipped.
  `AIController.Difficulty` is a three-tier enum, `AiTuning.For(tier)` carries the personality per
  tier, `ApplyDifficulty(savedIndex)` reads the saved setting, `ApplyDifficultyFromSettings` is
  called by `MatchInstaller` on every match, and the settings panel has the row. ⚠️ **`NoBotsIndex`
  is a fourth option meaning NONE**, asked for by name (*"make it so that in practice mode theres
  an option to turn off all bots"*), and it is an absence of SEATS rather than a parked brain.
  ⚠️ **The tier still clamps to 0..2 when NONE is selected**, deliberately, so an out-of-range cast
  can never reach `AiTuning.For`. **Do not rebuild any of this.** What is genuinely open below is
  the QUEUE half, not the bot half.
- ❌ **Bot backfill of an abandoned seat. CUT 2026-08-30, on 🧑's instruction:** *"we dont want
  bot backfill"*. A seat that empties mid-match stays empty. ⚠️ **This is a narrower cut than it
  looks and the code already does the narrow half**: `MatchRpc.HostPeerLeft` installs an
  `AIController` on a chair whose player dropped, gated on `AIController.BotsEnabled`, and
  `HostTakeSeatBackFromBot` hands it back on reconnect. That is a body nobody is driving being
  driven rather than a queue being padded, it keeps a 1-vs-3 from becoming a 0-vs-3, and it is
  not what was cut. **What is cut is backfill as a MATCHMAKING feature**: no bot is ever sent to
  a match to make up numbers, and no seat is filled by anything that was not in the lobby.
- ⚠️⚠️ **BOTS IN RANKED ARE ALLOWED WHEN THERE IS NOBODY TO PLAY, AND THIS REVERSES THE LINE
  THIS PHASE USED TO DRAW.** 🧑, 2026-08-30: *"im okay with bot showing up in rank if theres no
  ppl bcz no one plays this game yet anyways"*. The bullet that stood here read **"Never bots in
  ranked. Not once, not 'just to fill', not disclosed. That is the line."** It is overruled, by
  the person whose game it is, with the reason stated: a ranked queue that never fills is not a
  stricter ranked mode, it is no ranked mode.
  **Three things follow and they are not optional:**
  1. **The rating system has to know.** A result with a bot in it cannot move a rating the same
     amount as one without, or the fastest climb in the game is queueing at 4 a.m. Phase 9 owns
     the number; what it may not do is pretend the two are the same match.
  2. **The bot is labelled, in the lobby, on the scoreboard and in the match history.** That was
     already this phase's rule and it matters more here than anywhere else.
  3. ⏳ **THIS DECISION HAS AN EXPIRY AND THE REASON GIVEN IS THE EXPIRY.** *"no one plays this
     game yet"* is a statement about the population, so it stops being true the day the queue can
     fill itself. Re-ask then rather than treating this as settled for ever.
- **Bot fill in casual queue after a wait threshold**, disclosed clearly in the UI. A 45-second
  queue that ends in a playable match beats a 4-minute queue that ends in nothing.
- ❌ **No named practice ladder. CUT 2026-08-31.** A separate progression track against bots is a
  fourth bot feature and a fifth progression system, and Practice plus `GuidedTraining` already
  give a new player somewhere to learn.
- ✅ **Bots must be visibly labelled: DONE**, verified 2026-09-03. `BotFillRules.BotTag` is one
  string in the core and the lobby writes it. A player who thinks they beat a person and did not
  will be angrier when they find out than they would have been to know.
- ✅ **AND SO IS EVERYTHING ELSE IN THIS PHASE, INCLUDING THE PART THAT LOOKS HARDEST.**
  `BotFillRules` carries the 45-second casual threshold, the 150-second ranked one, and
  `Weight(humans, seats)`: **every human seat past the first is a quarter of the result**, so four
  humans is 1.0 and a solo human against three bots is 0.0. `RatingRules.Blend` applies it to
  rating, deviation AND volatility, and `ugs/cloud-code/match-record.js` computes the same weight
  from `IsBot` server-side. ⚠️ **Rule 1 above is therefore satisfied**, and `Phase11And12Tests`
  asserts both halves against one table. **Phase 11 has nothing open.** `docs/TODO.md` § 128.

⚠️ **AND THE HARD PART IS THAT THE BOTS ARE A BALANCE INSTRUMENT TOO.** `BotBehaviourProbe`'s
numbers are liveness floors, never comparisons at n=1, and `docs/TODO.md` § 16 carries the noise
floor. Do not tune bot difficulty off one run.

**The prompt for this phase is [§ 19.11](#1911-prompt-for-phase-11).** Every prompt in
this file lives in § 19 so there is one place to copy from. § 0.5 is the standing preamble each
one inherits and § 0.6 is what to re-verify before trusting any of them.

---

## PHASE 12 · MODES, MAPS AND CONTENT CADENCE

**A live game needs a reason to log in on a Tuesday, and new content is the most expensive answer
to that. So spend the cheap answers first.** Every mode below reuses the existing arena, rules and
art, which is why they are worth more per hour than a new map.

**⚠️⚠️ TWO EXTRA MODES, EVER. THE REST ARE CUT.** 🧑, 2026-08-31, on a list of seven proposals:
**nine modes splits thirty players nine ways**, and a mode nobody can fill is worse than a mode that
does not exist. Classic and Hero Strike are the game. These two are the whole arcade.

- ✅ **LAST TSINELAS STANDING.** Three tsinelas per attacker; lose them all and you are out; the
  last attacker takes the round. **The most different game available from parts that already
  exist**, which is why it earns the slot.
- ✅ **MIRROR.** Everyone gets the same character and tsinelas, rotated weekly. **The cheapest
  possible new mode**, one line of lobby logic, and a genuinely good competitive format.

**❌ Cut on 2026-08-31, all on population rather than on quality:**

- ❌ **Daily seed.** `INSPIRATION.md` § 2.9 has its longer history.
- ❌ **King of the Can.** Continuous rather than round-based, taya changes on knockdown.
- ❌ **Time attack.** Solo, scored on time. It was also the last place medals and ghosts made sense,
  and they go with it.
- ❌ **Survival.** Co-op against an escalating bot taya. It was the only co-op idea on the page and
  it is still cut: co-op needs its own balance pass and there is no population argument for it.
- ❌ **Sudden death.** Was proposed in `INSPIRATION.md` § 2.16 as a tie-breaker variant.
- ❌ **2v2.** The taya rotation does not support it and it is real design work rather than a switch.
  **Of everything cut here this is the one most likely to be worth revisiting**, because it is the
  only proposal that changes the social shape of a session rather than its rules.

**Maps:** three exist and one has a design document. A map is the most expensive content in the
game. **Map rotation and a map vote are nearly free and buy most of the same freshness.** Build
those before building a fourth map.

**Custom games** are the multiplier on all of it: private lobby, password, round length, score
target, character and tsinelas restrictions, bot count, item toggles. Community formats come out of
custom games for free, and it is also the tournament tool from Phase 17.

⚠️ **AND THE HONEST CONSTRAINT: FIVE STUDENTS CANNOT SHIP A HERO EVERY SEASON.** Plan a cadence
that is actually sustainable, which is roughly one substantial thing per season: a hero OR a map OR
a mode, plus cosmetics, plus balance. A missed cadence is worse than a slow one.

**The prompt for this phase is [§ 19.12](#1912-prompt-for-phase-12).** Every prompt in
this file lives in § 19 so there is one place to copy from. § 0.5 is the standing preamble each
one inherits and § 0.6 is what to re-verify before trusting any of them.

---

## PHASE 13 · SEASONS AND LIVE OPS

❌⚠️ **THERE IS NO RECURRING CHALLENGE CADENCE AT ALL NOW. WEEKLIES AND LOGIN STREAKS ARE CUT,
2026-08-31, ON PLAYER-FACING COMPLEXITY.** Dailies were already gone; weeklies were kept as "the
only recurring challenge cadence" and streaks as a gentler version of a streak. Both are cut
together, and the reason is not what they cost the team:

- ❌ **Weekly challenges.** A challenge list is a to-do list. It turns opening the game into
  reading homework and it quietly tells a player that the way they want to play is worth less than
  the way the list wants them to play. The game already has a reason to queue: the match.
- ❌ **Login streaks**, including the pausing kind. A streak's whole mechanism is making a missed
  day feel expensive, and the thought "I've broken it now" is the one immediately before somebody
  stops. A feature that punishes absence cannot also be the feature that survives absence.

**What is left of the recurring layer:**

- **A ten-week season**, which exists for the ranked soft reset and for an end-of-season summary
  card built to be screenshotted. ❌ **No season theme and no season track**, cut on 2026-08-31:
  § 4.1 records what a reward can actually cost to make.
- **A scheduled weekly hour**, from `INSPIRATION.md` § 2.24, and **LIGA NG BARANGAY**, the weekend
  side-picking event from § 2.10. Those two are the whole event calendar.
- **A live-ops calendar** in the repo, so the team knows what ships when and players can see it.

⚠️⚠️ **DAILY CHALLENGES ARE CUT, AND SO IS THE ROTATING FEATURED MODE. 2026-08-31.** The plan had
six recurring things running at once: dailies, weeklies, seasons, a featured mode rotation, the
weekly hour and Liga ng Barangay. **That is a live-ops calendar that normally has a full-time
person on it**, and this team is five students who also have to build the game.

- ❌ **Dailies** are the most maintenance for the least value once weeklies exist. Three challenges
  a day with a reroll is a content treadmill somebody has to feed forever, and a weekly does the
  same job at a seventh of the cost.
- ❌ **The featured mode rotation** has nothing to rotate. Phase 12 now ships two extra modes total,
  so a rotation would cycle between them and stop being a feature.

⚠️ **CHALLENGES DRIVE BEHAVIOUR AND BAD ONES DRIVE BAD BEHAVIOUR.** "Get 10 knockdowns" teaches a
player to ignore the can, which is the one thing the game is about. Write challenges against
outcomes the game wants: retrievals under pressure, rounds survived as last attacker, matches
completed, tags as taya.

### 13.1 ⚠⚠ WHAT THIS PHASE IS ACTUALLY FOR, THOUGHT THROUGH ON 2026-09-01

🧑 asked for this section to be reasoned out rather than inherited: *"thoroughly think on ur own
what should go on phase 13"*. **The cut list above is right and none of it is reopened.** What
follows is what is LEFT once the cuts are taken seriously, and it is four things rather than a
calendar.

**The starting position, measured rather than assumed:** Phase 9 shipped a ten-week season with a
soft reset (`RatingRules.SeasonAt`, `BeginSeason`, `SeasonOneStartUtc` = 2026-09-01, so **season 1
ends 2026-11-10**), and **nothing in the game says a season exists.** There is no season row, no
countdown, no end-of-season anything, and the reset happens inside `match-record.js` on the next
submission. That is the whole gap.

#### 1. A season has to be visible before it can be a reason to play

- **One row on the hub**, in the rank group: season number and days left. Not a screen.
- ⚠️⚠️ **THE SUMMARY CARD IS COMPUTED AT LOAD, NOT AT SUBMIT, AND THIS IS THE TRAP.**
  `beginSeason` runs inside `match-record.js` when a record is submitted, so **a player who does
  not play never crosses the boundary** and a player who does crosses it silently, mid-submission,
  with their old season already overwritten. The summary has to be built from the STORED profile
  the first time the game loads after the boundary, and the previous season's numbers have to be
  kept somewhere before the reset writes over them. **One field on the profile
  (`LastSeasonSummary`), written by the same code that resets.**
- **It shows once, and it is dismissible in one press.** A card that reappears is a nag.

#### 2. ⚠⚠ A SOFT RESET WITH SIX PLAYERS IS WORSE THAN NO SEASON, AND THIS IS THE ONE THAT WOULD ACTUALLY HURT

Glicko-2's soft reset pulls every rating 40 per cent toward the mean and widens every deviation.
That is correct for a ladder with a population. **This game's population today is the people in
one room**, and `FUTURE.md` § 11 already reversed a rule for exactly that reason (*"no one plays
this game yet"*). Resetting the only ladder anybody has, for six people, deletes the only record
of who is good and gives nothing back.

- **So the season ROLLS OVER rather than resetting when fewer than a threshold of accounts
  finished it ranked.** The number is a decision for 🧑; the shape is: count settled ranked
  accounts, and if it is under the threshold, extend the season and say so on the hub row.
- ⚠️ **It is the same expiry the bot rule has.** Both are population arguments and both stop being
  true on the same day, so they should be re-asked together.

#### 3. The calendar is data, and it must work with the cable out

**LIGA NG BARANGAY and the weekly hour are the whole event list**, and they are two recurring UTC
slots. The trap is building them as a service.

- ⚠️⚠️ **A CALENDAR THAT NEEDS A SERVER IS A CALENDAR THAT IS BLANK AT THE NATIONALS.** General
  Santos City is the reason `FUTURE.md` § 0.5 rule 7 exists and why CONTINUE AS GUEST may never
  touch the network. A table of recurring slots in `Packages/com.tumbangpreso.core/` answers *"what
  is on now, what is next, how long"* with no service, on every machine, identically. Cloud Code
  can override it later; it must never be required for it.
- **The event is only worth having if it is a QUEUE.** *"Liga ng Barangay, Saturday 8pm"* on a
  screen is an advert. The same line on the lobby with a countdown, and a QUICK MATCH that says
  *"the hour starts in 20 minutes"*, is a reason to be there. **Turning the calendar into a queue
  is the feature; the calendar is the data behind it.**

#### 4. Live ops without measurement is guessing, and the measurement already exists

Phase 3's telemetry is deployed and § 90.3's event names are a contract. Two events answer whether
any of this worked and neither is new machinery: **queue entries by hour of the week**, and
**matches completed per day**. Without them the weekly hour is a slot somebody chose and nobody
can say whether anyone came.

#### 5. What this phase must NOT grow

- ❌ **No battle pass, no season track, no theme.** Already cut, § 4.1 has the costing.
- ❌ **No challenge cadence.** Already cut, and the paragraph above is the standing rule for the
  day somebody reopens it.
- ❌ **No second currency.** Nothing in this game has a currency and a season is the usual place
  one gets introduced by accident.
- ⚠️ **And the summary card is a marketing asset, not only a player one.** Sponsors keep asking
  for material and this team keeps screenshotting the game by hand. A card built at 1920x1080 with
  the wordmark on it is the one artefact this phase produces that somebody outside the game will
  see.

**The prompt for this phase is [§ 19.13](#1913-prompt-for-phase-13).** Every prompt in
this file lives in § 19 so there is one place to copy from. § 0.5 is the standing preamble each
one inherits and § 0.6 is what to re-verify before trusting any of them.

---

## PHASE 14 · CONTROLLER SUPPORT ✅ SHIPPED 2026-09-02, branch `ui-redesign`

✅ **SHIPPED. `docs/TODO.md` § 125 is the as-built record and `CLAUDE.md` § 4a is the rule that
came out of it.** Read those two rather than this section: this was written ahead of the work and
§ 0.6 is why that matters.

**Starting point was zero**, exactly as written below: one keyboard binding and one mouse binding
per action, no gamepad paths, no control schemes.

| The plan | What shipped |
|---|---|
| Control schemes: Keyboard and Mouse, Gamepad, Touch | ✅ All three declared in the asset |
| Full gamepad bindings for every action, spectator context included | ✅ 26 bindings, generated FROM `InputLayer.InputCatalogue` by `InputAssetSync.Regenerate` rather than hand-written |
| ⚠️ *"`E` is contextual and that is the hard part"* | ✅ Still ONE control (`buttonWest`), still resolved downstream. The prompt was the real work and it reads the live binding per device now |
| Glyph swapping driven by the last device used, not a setting | ✅ `InputLayer.LastInputDevice` drives `Hud.KeyLabel`. ⚠️ The labels are WORDS ("BUTTON WEST"), not console face-button glyphs; that needs authored art and is § 125.13 |
| **Full menu navigation on a stick**, *"the thing that always gets skipped"* | ✅ **Not skipped.** `ScreenFocus` is installed by `MenuKit.BuildCanvas` and `ConvertedScreen.Start`, so every screen gets an explicit, wrapping focus path by construction, and `InputSurfaceProbe` walks it at twelve shapes |
| Rumble on knockdown, tag and can reset | ✅ **Done 2026-09-03**, `docs/TODO.md` § 126.7. Four cues, not three: BEING TAGGED is the strongest and is not on this list, because it is the one event that pays the player nothing and therefore the one the score system says nothing about. Two motors, `Max()` never sum, an off switch in the CONTROLS list, and `Rumble.Stop` on every exit path because a motor left running outlives the process |
| **No aim assist. Separate the pools instead** | ✅ Unchanged. `Matchmaker` already carried `InputDevice` in the pool key and still does |

⚠️⚠️ **AND THE THING THIS PHASE ACTUALLY TURNED ON WAS NOT A BINDING.** `StandaloneInputModule`
reads the LEGACY input manager, and this project runs `activeInputHandler: 2` (Both), so it ran
without erroring while no gamepad binding could reach it: a mouse worked, every screen looked
correct, and a stick moved nothing. `InputLayer.UiInputModule` replaces it and upgrades the five
scenes that ship an authored EventSystem. **A component that half works is worse than one that
throws.**

**The prompt for this phase is [§ 19.14](#1914-prompt-for-phase-14).** Every prompt in
this file lives in § 19 so there is one place to copy from. § 0.5 is the standing preamble each
one inherits and § 0.6 is what to re-verify before trusting any of them.

---

## PHASE 15 · MOBILE ⚠️ PLAYABLE 2026-09-02, NOT FINISHED, branch `ui-redesign`

⚠️⚠️ **THE HONEST STATUS: THE PORT BUILDS, INSTALLS AND RUNS, AND ITEMS 3 AND 6 BELOW ARE
UNTOUCHED.** `docs/TODO.md` § 125 is the as-built record. **This section's own opening line was
right and stays**: it is a port, not a feature, and calling it shipped because an .apk exists would
be the kind of claim `CLAUDE.md` § 2.2 exists to stop.

⚠️⚠️ **AND THAT SENTENCE WAS WRITTEN BEFORE IT WAS TRUE, WHICH IS EXACTLY WHAT § 0.6 IS ABOUT.**
On 2026-09-02 this section said the port *"builds, installs and runs"* and ticked item 1, while
`docs/TODO.md` § 125.13 said, in the same commit, **"THE .apk WAS NEVER BUILT OR INSTALLED, AND
NOTHING HAS RUN ON A DEVICE."** Two documents in one repository, one commit apart, flatly
contradicting each other about whether the headline deliverable existed. The TODO was the correct
one, which is § 0.5 rule 2's ordering (*"where this document and the code disagree, the code is
right"*) coming out exactly as written.

✅ **It is true as of 2026-09-03**, and `docs/TODO.md` § 126.10 is the receipt: the build line, the
install, the logcat, and two screencaps in `Logs/shots-android/`. ⚠️ **Reading that log found two
settings in `GameBuilder.ConfigureAndroid` that the engine had been silently refusing all along**,
one of which is the very claim item 1 makes below about x86_64. **A tick in this file is a plan,
not a measurement. Only `docs/TODO.md` records what was actually run.**

**Missing before a line was written**, and all of it now resolved: the Android module was not
installed **on either laptop** (a handoff said it was missing on the other machine; it was missing
here too). Installed through the Hub CLI with `--childModules`, which brings its own SDK, NDK and
OpenJDK. iOS still needs a Mac the team does not have.

1. ✅ **Module installed and a build put on a device.** `GameBuilder.BuildAndroid`, ARM64 and
   x86_64. ⚠️ **x86_64 is not optional here**: 🧑 has no Android handset (*"i dont have any nadroid
   at all"*), so an ARM64-only .apk could not be run by anybody on this team. The emulator AVD is
   `tumbangpreso` (Pixel 5, Android 14, x86_64).
2. ✅ **Touch controls**: stick, look drag, and a control per verb generated from
   `InputLayer.InputCatalogue`, laid out on two arcs around the thumb's rest (§ 125.10).
   ⚠️ **The contextual key did NOT become a long press with a radial fill** and should not: it is
   still one control resolved downstream, exactly as on a keyboard, because that is
   `PlayerInputReader`'s standing rule. A radial fill is a presentation idea and is still open.
   ➕ **Not in this plan and shipped anyway: a PUBG-style customiser** (opacity, size,
   drag-to-position), asked for during the work. § 125.11.
3. ❌ **Performance is UNMEASURED on device.** The inverted-hull outline still draws per prop.
   `docs/TODO.md` § 63 has what it costs. **Nothing in this batch touched rendering**, so assume
   the cost is exactly what § 63 says and measure before deciding.
4. ✅ **UI at phone aspect ratios**, and done the way this item asks rather than by eye:
   `ProbeResolutions` now carries `2340x1080`, `2400x1080` and his own `1600x680` window alongside
   the nine, and every layout probe drives all twelve.
5. ✅ **Crossplay with separate pools.** `NetSession.ProtocolVersion` untouched at 21 and asserted;
   `Matchmaker` still bands the ranked queue by `InputDevice`. Lobbies, join codes and LAN are
   cross-device.
6. ❌ **Battery, thermals and a 30 FPS cap option: not done.**
7. ✅ **Account continuity** was already Phase 1's and is unchanged.

⚠️ **THE PROTOCOL GATE PARAGRAPH BELOW IS STILL THE MOST IMPORTANT LINE IN THIS SECTION** and is
now enforced by a test rather than by remembering it.

⚠️ **THE PROTOCOL VERSION GATE IS AN ASSET HERE.** `NetSession.ProtocolVersion` already refuses
peers from different builds. Mobile and desktop must ship the same version at the same time or they
will refuse each other, correctly, and it will look like a bug.

**The prompt for this phase is [§ 19.15](#1915-prompt-for-phase-15).** Every prompt in
this file lives in § 19 so there is one place to copy from. § 0.5 is the standing preamble each
one inherits and § 0.6 is what to re-verify before trusting any of them.

---

## PHASE 16 · ACCESSIBILITY

**The game has none of this, and it is cheaper now than at any later point.**

### 16.1 ⚠️⚠️ The colourblind problem this game specifically has

`Art_Direction.md` § 1 is a law that never bends: **orange is OFFENSE, blue is DEFENCE**, and the
whole HUD, every role marker and every readability decision rests on it.

**Orange versus blue is the one pairing that survives red-green colourblindness**, which is about
8 per cent of men, so the art direction is accidentally excellent for the most common case. But it
is close to the worst possible pairing for **tritanopia**, and more importantly **the roles are
currently distinguished by hue alone**. A colourblind player, a bad projector at a tournament, or a
cheap phone screen all produce the same failure: you cannot tell the taya from the attackers.

**The fix costs almost nothing and helps everybody:** carry the role in a **second channel** as
well as hue. A shape on the nameplate, an icon on the marker, a different outline weight. Then add
palettes for deuteranopia, protanopia and tritanopia that keep the two roles maximally separated in
whatever the player actually sees.

⚠️⚠️ **STARTED 2026-09-03, AND CHECKING THE CLAIM FIRST CHANGED THE WORK. `docs/TODO.md` § 127.**
Two of the three surfaces named above already carried a second channel and nobody had noticed: the
scoreboard prints `DEFENDER` / `ATTACKER` as a word, and the floating tag writes `· TAYA` on the
defender alone. **The FLOOR RING was the hue-only one**, and it is the one that decides a fight,
because the tag fades out at twelve metres in a fourteen-metre box. The taya's ring is an annulus
now and an attacker's is still a disc, which is a shape rather than a colour and **spends less of
§ 2's area budget rather than more**. ⚠️ **The crosshair and the lata label are still hue-only**,
and the acceptance test, a greyscale frame, has not been run yet. § 127.3 has both.

### 16.2 The rest of the accessibility list

Every one of these is small, and together they decide whether some people can play at all.

- UI scale and a larger-text option. The HUD is already nine-patch, so this is mostly layout.
- Full rebinding for every action, which `Rebinding` mostly gives already, plus **hold versus
  toggle** for sprint and for the contextual grab.
- FOV slider, camera shake slider, motion blur off, and a **reduced-effects mode** that also
  doubles as the low-end performance mode. `VISION.md` § 2's readability budget is the same
  argument from the other side.
- Subtitles and captions for callouts and ability sounds, which also help anybody playing muted.
- A high-contrast HUD option.
- Flash and strobe reduction, which `AbilityShowcaseProbe`'s 12 per cent screen-white bound already
  half implements. Extend that probe to assert the reduced mode is genuinely calmer.
- Colour-independent slipper highlights: the landed and owner rims are already player-chosen
  colours, so add a shape or a pulse as the second channel.

### 16.3 Localisation ❌ CUT 2026-08-31

**There is none: no language setting, no string table, every string inline. It stays that way.**

🧑, on a plan to ship English, Tagalog and Cebuano: *"english only"*.

⚠️ **The cost was never the translation, it was the forever.** Extracting every user-facing string
into a table is a week of boring work; keeping three languages in step for every screen, every
challenge name, every title and every character added after that is a permanent tax on shipping
anything. **English only removes the tax entirely.**

⚠️⚠️ **AND HERE IS THE ONE THING TO KNOW IF THIS IS EVER REVISITED: the string table gets more
expensive every month, not less.** Extracting strings is cheap now and painful at two thousand of
them. If Tagalog is ever wanted, do the extraction first as its own small job and ship it with one
language in it, rather than trying to do both at once under a deadline.

**What survives:** § 16.1 and § 16.2, the accessibility half, which is a separate argument and is
not cut.

**The prompt for this phase is [§ 19.16](#1916-prompt-for-phase-16).** Every prompt in
this file lives in § 19 so there is one place to copy from. § 0.5 is the standing preamble each
one inherits and § 0.6 is what to re-verify before trusting any of them.

---

## PHASE 17 · TOURNAMENTS, LAN, SPECTATING AND REPLAYS

✅ **THE UNPLUGGED LAN RUN HAS BEEN DONE, confirmed by 🧑 on 2026-08-31.** This paragraph was the
one genuinely urgent item in the whole file and it is closed. **Do not re-raise it as outstanding.**

⚠️ **The requirement it protects is permanent, though.** The nationals are in General Santos City
and venue internet cannot be assumed: a tournament build that needs UGS to start a match is a
tournament build that can fail in the room. A full four-player match must stay startable and
completable entirely on LAN with the router unplugged, and **every screen between the menu and the
match must survive UGS being unreachable**. That is a regression to guard now rather than a gap to
close. ⚠️ **The account layer is the most likely thing to break it**, since a boot that waits on
UGS is precisely what an unplugged venue produces; `docs/TODO.md` § 88.1d is the bound that stops
that, and § 88 is the phase to re-test after.

**What exists:** `SpectatorCamera` with free, follow and POV modes, a spectator pause that crosses
the wire, a HUD that already knows how to draw a broadcast clock, and `LobbySession`'s reconnect.
**This phase is closer to done than any other here.**

- **Tournament mode:** lobby password, fixed roster, fixed map, no matchmaking, spectator slots
  that do not consume a seat, and a match that can be restarted by an organiser.
- **Replays: record the input stream and the seed, not the frames.** The match is deterministic
  from `InputIntent` because a bot presses the same buttons a human does and one physics step
  serves both. That makes a 200 KB replay possible instead of a 2 GB video, and it is a genuine
  advantage this codebase already has. **Prove determinism with a test before building on it.**
- **Clip export:** a "save the last 30 seconds" key. This is the growth engine. A game nobody can
  clip is a game nobody posts.
- **Caster overlay:** four scores, four stamina bars, ultimate charge, nameplates, laid out for a
  stream rather than for a player.
- **Spectator delay**, configurable, so a stream cannot be sniped in a tournament.
- **Highlight detection** off the events already raised, so the clip worth posting can be found
  without watching the whole match.
- **A bracket page**, which can be a static site and needs no service.
- **An organiser's checklist document**, because the thing that loses a tournament is a build
  mismatch: `NetSession.ProtocolVersion` refuses peers from different branches by design, so every
  machine in the room must be on the same .exe and somebody has to be responsible for that.

**The prompt for this phase is [§ 19.17](#1917-prompt-for-phase-17).** Every prompt in
this file lives in § 19 so there is one place to copy from. § 0.5 is the standing preamble each
one inherits and § 0.6 is what to re-verify before trusting any of them.

---

## PHASE 18 · GETTING IT IN FRONT OF PEOPLE

- **itch.io first.** Free, no gatekeeper, and where a game like this finds its first hundred
  players. Automate the release with butler from a GitHub Action.
- **A WebGL build in the browser.** The module is already installed. One click from a link converts
  far better than a download, and this game is small enough to actually work there.
- **Steam is 100 USD**, the only unavoidable cost anywhere in this document, and worth it only once
  there is a reason for people to arrive.
- **A trailer that shows a retrieval and a near miss**, not a montage of throws. `VISION.md` § 0:
  the tension is the retrieval.
- **A Discord**, which is free, and is where a game this size actually retains its players. One
  channel for finding a fourth is worth more than any matchmaking work at low population.
- **The Filipino angle is the marketing and it is not a gimmick.** Nobody else has made this.
- ⚠️ **No vendor or middleware names in any public material**, per the standing rule.

**The prompt for this phase is [§ 19.18](#1918-prompt-for-phase-18).** Every prompt in
this file lives in § 19 so there is one place to copy from. § 0.5 is the standing preamble each
one inherits and § 0.6 is what to re-verify before trusting any of them.

---

## Appendix A · Ideas ranked by value per hour of work

**Do these. They are all small and they all punch far above their cost:**

- **A post-match "best moment" card**, generated from events already raised, designed to be
  screenshotted. Almost free, and it is the thing that gets posted.
- **Nameplates**: title, emblem, hero mastery number, visible in the lobby and on the scoreboard. The
  cheapest status system that exists and people care about it enormously.
- **A career page that is one screenshot.** Peak rank, best match, favourite character, hours.
- **Endorsements**, Overwatch style: after a match, endorse one opponent, level shown on the
  nameplate. The only anti-toxicity system that has ever measurably worked, and it costs one button.
- **A ping and comm wheel.** Does most of what voice chat does for none of the cost, none of the
  moderation liability and none of the toxicity.
- **A practice range** with a can, a ghost defender and a retrieval trainer. `GuidedTraining`
  already exists to build on.
- ~~The daily seed~~. ❌ Cut 2026-08-31 on scope. `INSPIRATION.md` § 2.9.
- **"Save last 30 seconds" clip export** from Phase 17. The single best growth feature here.
- **A first-match funnel fix**, whatever Phase 3 says it is. Cheaper than every feature above and
  usually worth more than all of them.

**Do these later:**

- Ability variants shown as a build on the profile, so people argue about builds.
- Map rotation and map voting.
- Achievements, internal now and on a store later.
- Photo mode, which is free marketing.
- Dynamic music that reacts to the round state, and a Filipino soundtrack angle.
- A creator mode for maps, which is the largest possible project and the only one that makes the
  game outlive the team.

**Do not do these:**

- ⚠️ **Loot boxes, gacha or anything with a purchase.** There is no budget, so there is no payment
  processing, and a game whose progression is honest is worth more to this team than a monetisation
  model it cannot legally operate.
- ⚠️ **Power that unlocks.** Said three times in this document because it cannot be undone after
  players have ground for it.
- ⚠️ **Voice chat.** Cost, moderation liability, and a ping wheel does the job for a fraction of
  everything.
- ⚠️ **A fourth map before map voting exists.** Voting buys most of the same freshness for a tiny
  fraction of the work.
- ⚠️ **Uploaded profile photos before there is somebody whose job is to moderate them.** § 1.4.

## Appendix B · What kills this, ranked by how likely it is

1. **Population.** A 4-player game at 30 concurrent players has a queue problem that no ranked
   polish fixes. **Phase 11 is the mitigation and it is why bots and backfill are a phase rather
   than a footnote.** Also why the Discord in Phase 18 is not decoration.
2. **Too much game, which is not the same as too much work.** Every phase here is weeks, and doing
   three of them well beats starting eight. ⚠️ **But the failure this describes is a player opening
   a profile with eleven progress bars on it, not a team running out of hours.** § 0.5 rule 11b is
   the cutting criterion and `INSPIRATION.md` § 10.3 is what has already gone on it. Appendix C is
   the honest order.
3. **Cheating, discovered after ranked ships.** Phase 8 before Phase 9 exists for this. A ladder
   that loses trust does not get it back.
4. **The Relay free tier.** It is the first thing that will actually run out. Watch the bandwidth
   number from the day Phase 7 ships and know what the fallback is before it matters.
5. **Progression that touched power.** Recoverable only by taking something away from players who
   earned it, which is the worst thing a live game can do.
6. **A tournament lost to a build mismatch or venue internet.** Phase 17's urgent half.
7. **The team burning out on live ops.** A cadence that is announced and missed is worse than a
   slower one that is kept.

## Appendix C · The order to actually do it in, if there is one month

1. **Phase 1**, accounts. About a week, and it unblocks everything.
2. **Phase 2**, profile, stats and match history, plus **Phase 3**'s first-launch funnel. A week.
3. **Play it for a week and read the funnel.** Fix what it says, not what feels next.
4. **Phase 4**, XP, levels and the free track. This is the phase that makes a player come back
   tomorrow, and it needs no matchmaking, no ranked and no integrity work to be worth having.
5. Then choose between **Phase 7 plus 8 plus 9** (the competitive spine, all three or none) and
   **Phase 5** (customisation, more fun per hour, no integrity risk).

⚠️ **RANKED IS THE MOST EXCITING PHASE AND IT SHOULD NOT BE FIRST.** It needs matchmaking under it,
integrity beside it and a population to fill it. Built too early it is a ladder with four people on
it, and an empty ladder is the fastest way to make competitive play feel dead.

✅ **THE ITEM THAT USED TO BE URGENT HERE IS DONE.** This read "confirm the game runs a full
four-player match on LAN with the internet unplugged, before the nationals". 🧑 confirmed on
2026-08-31 that it has been run. **What remains is keeping it true through every phase above**,
which is a regression check after each one rather than a task waiting to be started.
---

## 19 · THE PROMPTS

**One place to copy from.** Every prompt below is written to be pasted into a fresh session as its
entire brief. They are deliberately uniform: same opening, same shape, same closing, so a session
that has run one knows how to run any of them.

⚠️ **Each one names § 0.5 rather than repeating it.** That is on purpose: nineteen copies of the
same rules is nineteen copies to fix. The prompt tells the session to go and read it, which it will.

⚠️ **And each one carries a VERIFY FIRST block**, because these were written on 2026-08-31 against
a codebase that keeps moving. A prompt that turns out to be wrong is handled by § 0.5 rule 11, not
by building it anyway.

| Prompt | Phase | Depends on | Rough size |
|---|---|---|---|
| [§ 19.1](#191-prompt-for-phase-1) | Accounts and identity | Nothing | About a week |
| [§ 19.2](#192-prompt-for-phase-2) | Profile, stats, match history | 1 | About a week |
| [§ 19.3](#193-prompt-for-phase-3) | Telemetry | 1 | A day, do it with 2 |
| [§ 19.4](#194-prompt-for-phase-4) | XP, levels, mastery | 1, 2 | Days |
| [§ 19.5](#195-prompt-for-phase-5) | Cosmetics and customisation | 1, 2, 4 | Over a week, mostly content |
| [§ 19.6](#196-prompt-for-phase-6) | Social, friends, parties | 1 | Days |
| [§ 19.7](#197-prompt-for-phase-7) | Matchmaking | 1, 2, 6 | Days |
| [§ 19.8](#198-prompt-for-phase-8) | Competitive integrity | 2 | Days. **Blocks 9.** |
| [§ 19.9](#199-prompt-for-phase-9) |  Ranked | 1, 2, 6, 7, **8.1** | About a week |
| [§ 19.10](#1910-prompt-for-phase-10) | Loadouts and achievements | 4, 5 | Weeks, mostly content |
| [§ 19.11](#1911-prompt-for-phase-11) | Bots, backfill, population | Nothing hard | Days |
| [§ 19.12](#1912-prompt-for-phase-12) | Modes, maps, custom games | Nothing hard | Weeks |
| [§ 19.13](#1913-prompt-for-phase-13) | Seasons and live ops | 2, 4 | Days |
| [§ 19.14](#1914-prompt-for-phase-14) | Controller | Nothing | About a week |
| [§ 19.15](#1915-prompt-for-phase-15) | Mobile | 14 | **The biggest item here** |
| [§ 19.16](#1916-prompt-for-phase-16) | Accessibility | Nothing | Days |
| [§ 19.17](#1917-prompt-for-phase-17) | Tournaments, LAN, replays | Nothing | ⚠️ **Its first step is urgent** |
| [§ 19.18](#1918-prompt-for-phase-18) | Distribution | A build worth shipping | Days |

---

### 19.0 PROMPT ZERO: refresh this plan before using it

**Run this first if it has been more than a month, or if any prompt below looks wrong.** It costs
one short session and it is cheaper than building a phase against a stale brief.

> Read `CLAUDE.md`, then `docs/VISION.md`, then `docs/TODO.md`, then `docs/FUTURE.md` §§ 0.5 and
> 0.6, then `docs/INSPIRATION.md` § 0. They carry the rules of the repo, what the game is for, what
> is open, and the standing rules for this task. Do not skip them because this prompt summarises
> the task; the summary is not the rules.
>
> **Task.** `docs/FUTURE.md` and `docs/INSPIRATION.md` are plans written on 2026-08-31 ahead of the
> work they describe. Bring their factual claims back in line with the code, and change nothing
> else.
>
> Work through the table in `docs/FUTURE.md` § 0.6 row by row and run each check. Then sweep both
> documents for anything else that has moved: content counts, file and class names, section
> numbers in other documents, package versions, service names, and any phase that has partly or
> wholly shipped since. For each phase that shipped, mark its heading `✅ SHIPPED <date>`, move its
> numbers into `docs/Design.md` or `docs/TODO.md` where they belong, and **leave the phase text in
> place**, because the reasoning is the part that stays valuable.
>
> ⚠️ **Correct facts. Do not rewrite arguments.** If the design reasoning in a phase now looks
> wrong to you, say so at the top of your handoff and leave the text alone. A session refreshing
> facts is not the session that gets to change the plan.
>
> ⚠️ **Do not quote a vendor's free-tier quota anywhere**, in these files or to anybody. They
> change without notice. Name the service and what runs out first, never the number.
>
> **Done when:** every row of § 0.6 has been checked and the result recorded, both documents match
> the code, `docs/TODO.md` records what changed, and it is committed and pushed. No `.cs` file is
> touched by this task.

---

### 19.1 Prompt for Phase 1

**Accounts and identity. Nothing else in this plan works without it.**

> Read `CLAUDE.md` first, then `docs/VISION.md`, then `docs/TODO.md`, then `docs/FUTURE.md` §§ 0.5
> and 0.6, then `docs/FUTURE.md` § 1. They carry the rules of the repo, what the game is for, what
> is open, the standing rules every prompt in that file inherits, and this phase's brief. Do not
> skip them because this prompt summarises the task; the summary is not the rules.
>
> **VERIFY FIRST.** This brief was written 2026-08-31. Check these before acting:
> `grep authentication Packages/manifest.json` shows the UGS authentication package installed,
> `grep -rn "AuthenticationService" Assets` shows the boot/session owner, and
> `grep -rn "PlayerAccount" Assets` shows whether Phase 1 has started or shipped. Read
> `docs/TODO.md` § 88 before rebuilding anything. If any of those has changed, follow § 0.5 rule 11.
>
> **Build the account layer.**
> 1. Anonymous sign-in on first launch, silently, before the main menu is interactive. No prompt,
>    no form, no blocking UI. A player who never makes an account still gets a stable id, a profile
>    and progression.
> 2. An optional upgrade to username and password, offered at the moment the player first earns
>    something worth keeping rather than buried in settings, preserving everything the anonymous
>    account had.
> 3. Sign-in on a second device by username, migrating progress.
> 4. Session persistence, so a returning player is signed in before the menu draws.
> 5. A `PlayerAccount` service beside `NetSession` owning the id, display name, discriminator, bio,
>    country, pronouns and signed-in state, raising an event when any change. ❌ No privacy flags:
>    profiles and history are public, cut on 2026-08-31, § 1.3.
> 6. Route the lobby's player name through it. Today the name is whatever the peer sends, and the
>    first thing anybody does with a new account system is impersonate somebody.
> 7. Account deletion. Build it now, where it is an afternoon, not after
>    launch where it is a migration. ❌ Data export is cut until somebody asks: § 1.5.
>
> **Two decisions that are already made, with reasons in § 1.4 and § 1.2. Do not relitigate them
> without saying why in your handoff.**
> - **The avatar is an in-game portrait composed from ids and rendered through `ModelPreview`, not
>   an image upload.** Uploads mean permanent content moderation run by five students, storage and
>   bandwidth against a free tier forever, and a photograph sitting beside a voxel cast.
> - **Email is optional and is recovery only.** It is never required to play. An address is
>   personal data and requiring one at first launch is the single largest drop-off point a game
>   this size has.
>
> **Constraints.** Everything degrades to a local-only profile when the service is unreachable,
> including LAN, joining by code, Practice and Training: § 0.5 rule 7, and § 17 for why. Name
> validation, discriminator allocation and local-versus-remote precedence go in
> `Packages/com.tumbangpreso.core/` with tests.
>
> **Done when** a fresh install reaches the menu signed in with no prompt, the id survives a
> restart, a username attaches later without losing anything, an account can be
> deleted, a LAN match still starts with the network cable pulled, and § 0.5 rule 9 is satisfied.

---

### 19.2 Prompt for Phase 2

**The profile, the stats and the match history.**

> Read `CLAUDE.md` first, then `docs/VISION.md`, then `docs/TODO.md`, then `docs/FUTURE.md` §§ 0.5
> and 0.6, then `docs/FUTURE.md` § 2. Do not skip them because this prompt summarises the task; the
> summary is not the rules.
>
> ✅ **PHASE 2 SHIPPED 2026-08-30. THIS PROMPT IS KEPT AS THE RECORD OF WHY IT IS SHAPED THE
> WAY IT IS**, per § 0.6's maintenance rule. What actually landed is `docs/TODO.md` § 89, and
> it departs from the text below in three places: **§ 89.2** ("last attacker" is an
> interpretation, because this game eliminates nobody), **§ 89.3** (each peer submits its own
> line, so the cost is one call per player per match) and **§ 89.4** (four § 2.1 items wait on
> later phases). Read those before re-running any of this.
>
> All four checks below were run on 2026-08-30 and all four passed: `player-account` is listed by
> `ugs cloud-code scripts list`; the `Ugs` category returned **4/4 with `total="4"`** at preflight
> and **5/5** afterwards, once this phase added `TheCareerEndpointAnswersALoad`; `ScoreEvent` and
> `AddScore` are unchanged and still the single host-side writer; and every stat in § 2.2 exists,
> with clutch rate derived at read time exactly as check 4 says.
>
> **VERIFY FIRST. ⚠️ PHASE 1 IS NOT FULLY SHIPPED AND THIS PROMPT USED TO ASSUME IT WAS.**
> The client half landed at `f8b47d01` on 2026-08-31; the SERVICE half did not, and one of
> the build steps below depends on it. Check all four before planning. ⚠️ **That commit was on a
> branch that is now DELETED and folded into `profile-stats`**, which is the only live branch;
> read it out of `profile-stats`, and do not go looking for a branch by name here.
>
> 1. `grep -rn "PlayerAccount" Assets` finds the service, and `AccountRules` is in
>    `Packages/com.tumbangpreso.core/`. If not, Phase 1 has moved; read `docs/TODO.md` § 88 first.
> 2. ✅ **The `player-account` Cloud Code endpoint IS deployed and active** as of 2026-08-31, on
>    project `dcf0831e-a5f4-43b4-832e-b687f13a3569` under org `matthewtlabrador`. Confirm with
>    `ugs cloud-code scripts list`, and prove it end to end with
>    `-testCategory "Ugs"`, whose `TheAccountEndpointAnswersALoad` calls it with a real bearer
>    token and got `{"output":{"profile":""}}` back. ⚠️ **Do not work around a failure here by
>    writing Cloud Save from the client**, which § 0.5 rule 6 forbids; fix the deploy instead.
> 3. `MatchDirector` still raises the scoring events this phase reads. `ScoreEvent` in
>    `MatchRules.cs` is the enum; `AddScore` is still the single host-side writer per `CLAUDE.md`
>    § 4 and this phase must not change that.
> 4. The stat list in § 2.2 still matches the verbs the game has. As of 2026-08-31 every one of
>    them exists except **clutch rate**, which is derived at read time rather than raised as an
>    event, so do not go looking for a `Clutch` event.
>
> **⚠️ IF THE ENDPOINT HAS SINCE BEEN BROKEN OR THE PROJECT RELINKED AGAIN**, per § 0.5 rule 11:
> build steps 1, 3, 4, 5, 6 and 7, which are the majority of the phase and none of which need it.
> Write step 2's call site behind the same local-queue path step 6 already needs, so deploying
> later switches it on rather than retro-fitting it. **Say so at the top of the handoff rather
> than reporting the phase as done.**
>
> **Build.**
> 1. A `PlayerProfile` document: identity, level, XP, career totals, per-mode records,
>    per-character records, inventory, rank.
> 2. A `MatchRecord` carrying the whole four-player scoreboard, **written once per match by one
>    writer**, through a Cloud Code endpoint. Write it through the endpoint from day one even
>    though it is spoofable until Phase 8, because retro-fitting the call site later is the
>    expensive half.
> 3. Collect the stats in § 2.2 host-side off events that already exist, and submit ONE payload at
>    match end. Never one call per event: § 0.3 says why.
> 4. The profile screen to the layout in § 2.1, using the existing UI kit and nine-patch art and
>    following `ConvertedScreen`'s conventions rather than inventing a screen type.
> 5. The end-of-match summary showing what this match added.
> 6. The offline path: a match played with no connection queues its record locally and submits on
>    the next sign-in.
> 7. Keep 100 full records per player and roll older ones into totals.
>
> **Constraints.** The record and profile shapes are engine-free types in
> `Packages/com.tumbangpreso.core/` with tests, so both toolchains compile them and the shape is
> asserted without an editor. ⚠️ **Do not show a stat you would not defend in an argument**: if a
> stat is noisy at low sample size, hide it until the sample supports it and say so in the UI.
>
> **Done when** finishing a match writes exactly one record, the profile updates without a reload,
> career totals survive a reinstall on the same account, the whole match costs one endpoint call,
> and § 0.5 rule 9 is satisfied.

---

### 19.3 Prompt for Phase 3

**Telemetry. Small, and it decides what to build next. Do it alongside Phase 2.**

> Read `CLAUDE.md` first, then `docs/VISION.md`, then `docs/TODO.md`, then `docs/FUTURE.md` §§ 0.5
> and 0.6, then `docs/FUTURE.md` § 3. Do not skip them because this prompt summarises the task; the
> summary is not the rules.
>
> **VERIFY FIRST.** Check whether an analytics package is already present and whether any events
> are already sent, so this does not become a second system.
>
> **Build.**
> 1. **The first-launch funnel first**, before anything else: launch, sign-in, main menu, first
>    queue, first match started, first match FINISHED. It is about forty lines and it is the single
>    most valuable number in this plan.
> 2. Then the match-level events in § 3.
> 3. Batched and sent once per session, never per event.
> 4. A visible opt-out in Settings that is honoured completely, and a plain statement of what is
>    collected.
>
> **Constraints.** No personally identifying field in any event, ever. Keep payloads small and
> event names stable: ⚠️ **a renamed event is a broken history**, so choose names once and write
> them into `docs/TODO.md` as the contract.
>
> **Done when** a full session produces one batch, the funnel can be read end to end for a new
> install, the opt-out actually stops all sending, and § 0.5 rule 9 is satisfied.

---

### 19.4 Prompt for Phase 4 ✅ SHIPPED 2026-08-30

⚠️ **KEPT AS THE RECORD OF WHY THE PHASE IS SHAPED THIS WAY, per § 0.6's maintenance rule.**
What actually landed is `docs/TODO.md` § 91, and step 1 below is the one that departs from it:
the AFK check reads MOVEMENT rather than `InputIntent`, because the host never receives a remote
player's intent. Do not re-run this prompt against the shipped code.

**XP, levels and per-character mastery. This is the phase that makes a player come back
tomorrow, and it needs no matchmaking and no ranked to be worth having.**

> Read `CLAUDE.md` first, then `docs/VISION.md`, then `docs/TODO.md`, then `docs/FUTURE.md` §§ 0.5
> and 0.6, then `docs/FUTURE.md` § 4. Do not skip them because this prompt summarises the task; the
> summary is not the rules.
>
> **VERIFY FIRST.** Phases 1 and 2 shipped, and the Cloud Code endpoint that writes the match
> record exists, because XP is computed there and never sent by a client.
>
> **Build, in this order.**
> 1. **The AFK check first.** The moment completion pays, standing still pays. Detect a seat that
>    has not acted for a whole round using the input telemetry the bots already produce, pay it
>    nothing, and escalate on repeats. Do not build XP before this exists.
> 2. Account XP from completion, placement and a small set of per-match objectives, weighting
>    completion heavily and placement lightly, so leaving is the only thing that costs.
> 3. Account level, uncapped, a new border every 50.
> 4. Per-hero mastery. ⚠️ **The six heroes only**, narrowed 2026-08-31: § 10 has the reasoning.
>    The other twelve characters get a played count and no path.
> 5. ❌ **No soft currency and no shop.** Cut on 2026-08-31: rewards come straight off the track and
>    mastery. Do not add an economy.
>    ⚠️ **A leftover step reading "a soft currency earned per match" sat directly under this line
>    until 2026-08-31**, so the prompt cut the economy and then told the next session to build it.
>    If a step here ever contradicts a ❌ above it, the ❌ is the decision.
> 6. ❌ **No season track.** Cut on 2026-08-31: § 4 has the reasoning and § 4.1 has the thing to
>    read before designing any reward at all, which is what a reward can actually cost to make.
> 7. The end-of-match XP bar and the mastery screen, existing UI kit.
>
> **Constraints.** Every curve and reward table is data in `Packages/com.tumbangpreso.core/` with
> tests, including one that asserts **no reward on any track changes a gameplay number** (§ 0.5
> rule 4). ⚠️⚠️ **THE XP RATE IS FLAT.** No diminishing returns, no rested bonus, no daily cap. A
> match pays what a match pays, for everybody, forever. Two rate curves were proposed and both were
> cut on 2026-08-31, and § 4 records why: the problem they solve does not exist in a game where
> nothing on any track affects a match.
>
> **Done when** a match awards XP computed server-side from its record, an AFK seat earns nothing,
> the track pays out, no reward touches a gameplay value and a test proves it, and § 0.5 rule 9 is
> satisfied.

---

### 19.4b Prompt for Phase 4.5

**Quality control across phases 1 to 4. Not new features: proof that the four that shipped
actually do what their acceptance lists claim.**

> Read `CLAUDE.md` first, then `docs/VISION.md`, then `docs/TODO.md`, then `docs/FUTURE.md`
> §§ 0.5 and 0.6, then `docs/FUTURE.md` § 4.5. Do not skip them because this prompt summarises
> the task; the summary is not the rules.
>
> **VERIFY FIRST.** Phases 1, 2, 3 and 4 have all shipped. `docs/TODO.md` §§ 88 to 92 are the
> as-built records and they are the thing you are checking, not the thing you are trusting.
>
> **Build, in this order.**
> 1. **The server-side XP assertion first**, because it is the one check that catches the two
>    halves of `ProgressionRules` drifting apart in the place a player would notice. Submit a real
>    `MatchRecord` through `match-record`, read the profile back, and assert the XP moved by
>    exactly `ProgressionRules.MatchXp`. ⚠️ Use a record whose `MatchId` is unique per run or the
>    idempotency guard will refuse the second one and the test will pass by not counting anything.
> 2. **A probe case per endpoint ACTION**, nine of them. ⚠⚠ Per `docs/TODO.md` § 90.5, each
>    assertion must be a string only the intended branch can produce. "It answered" is what let
>    three broken actions ship green.
> 3. **Walk every acceptance bullet in §§ 1 to 4** and give each one a named test or a named
>    reason it cannot have one. Correct the plan file where it disagrees with the code, same
>    commit, per § 0.5 rule 2.
> 4. **A layout probe for `MatchResult`'s XP block** and for the settings telemetry row, the two
>    phase surfaces `PlayerHubLayoutProbe` does not reach.
> 5. **The offline walk**, per § 0.5 rule 7: boot, menu, hub, career, LAN match, end-of-match bar,
>    and the queue flushing on the next sign-in, with the cable out. Automate what can be
>    automated and add the rest to `docs/TODO.md` § 90.4's list of what to check by hand.
> 6. **Write the deferred list** into `docs/TODO.md` so nothing absent-by-design gets refiled as a
>    bug in three weeks.
>
> **Constraints.** ⚠⚠ **This is not a redesign and not a balance pass.** Every progression
> number is an unmeasured starting point and § 91 says so of each; moving them belongs to whoever
> has a week of telemetry. Do not add a feature to make a test easier. New rules still go in
> `Packages/com.tumbangpreso.core/` with tests, per § 0.5 rule 3.
>
> **Done when** every acceptance bullet has a named test or a named reason, every endpoint action
> has a branch-specific assertion, a real submission's XP is asserted against the core's own
> arithmetic, and § 0.5 rule 9 is satisfied.

---

### 19.5 Prompt for Phase 5

**Cosmetics, the inventory, 3-slot Custom Character Creator, and hero outfits.**

> Read `CLAUDE.md` first, then `docs/VISION.md`, then `docs/TODO.md` § 107, then `docs/FUTURE.md` §§ 0.5
> and 0.6, then `docs/FUTURE.md` § 5 and `docs/wearables_catalog.md`. Do not skip them because this
> prompt summarises the task; the summary is not the rules.
>
> **VERIFY FIRST.**
> ⚠️ **ROSTER INTEGRITY IS THE HARD RULE AND IT IS ENFORCED IN ONE FUNCTION.** The twelve
> Classic characters and the six heroes (DANTE, CHESKA, SEAN, ZACK, NEMU, PHAISTER - **not
> Berto, who is Classic**) have canonical skin and faces. `PaletteRules.IsProtectedSlot` holds the
> face slot and the three skin slots out of every recolour, on both sides of the wire. Cosmetics for
> a named character are clothes and colour, never skin.
>
> **Build.**
> 1. **The 3-Slot Custom Character Creator**: A dedicated "Create Your Own Character" slot allowing
>    players to build and save up to 3 custom avatars (storing facial expressions, natural Filipino
>    skin tone, height/build, hairstyle/color, streetwear, accessories, tsinelas, and lata), selecting
>    one active custom character to bring into matches.
> 2. **The Hero Outfits & Thematic Skins**: Optional cosmetic clothing swaps for roster heroes.
> 3. **The Unified Banner**: Frame, pose, badge, title, and 3 stat trackers.
> 4. **No Currency, No Shop**: Free unlocks via account level, hero mastery, achievements, and ranked tiers.
>
> **Constraints:**
> - ⚠️⚠️ **STRING IDS FOR EVERY COSMETIC, NOT WIRE INDICES.**
> - ⚠️ **A cosmetic must never change a silhouette enough to change a read.** Bound the wearable volume.
> - ⚠️ **Warm skin tones only for the custom character**: 32 of them in
>   `CustomCharacterRules.SkinToneNames`, every one warm, with the hex carried in the name so there
>   is one list rather than a list and a colour table that can disagree.
> - Extend `RosterEntryAsset` and `RosterBook`; do not build a parallel content system.
> - Preview through `ModelPreview` with the real shader, never a flat icon.
>
> **Done when** a custom character can be built in one of 3 slots, equipped into a match, seen by
> all peers, and § 0.5 rule 9 is satisfied.
>
> ⚠️⚠️ **WHAT IS DONE AND WHAT IS NOT, AS OF 2026-08-31.** The screen, the three slots, the
> persistence, the live preview, the roster lock, the whole voxel wardrobe and the borrowed hero kit
> are built (`docs/TODO.md` § 108 and § 110). **The custom character does not yet cross the wire and
> does not yet walk into a match**: `CustomCharacterStore.ActiveWire` produces the string and
> nothing sends it, `MatchInstaller` still spawns a roster entry, and `HeroAbilitySystem.CreateKitFor`
> does not yet read `HeroKitId`. That is the remaining half of this phase and it is § 110.8.

---

### 19.6 Prompt for Phase 6

**Friends, presence, parties, blocking.**

> Read `CLAUDE.md` first, then `docs/VISION.md`, then `docs/TODO.md`, then `docs/FUTURE.md` §§ 0.5
> and 0.6, then `docs/FUTURE.md` § 6. Do not skip them because this prompt summarises the task; the
> summary is not the rules.
>
> **VERIFY FIRST.** Phase 1 shipped. Read `LobbyChat` before writing any chat code and read
> `ServerQuery`'s query interval before adding any polling.
>
> **Build.** Friends by id, by name and tag, and by share code. Presence: online, in menu, in
> queue, in a match, spectating. Parties of 2 to 4 that queue together. Invites from the friends
> list **and from the end-of-match screen**, which is the highest-converting social prompt a game
> of this shape has. Recent players with a one-click add. Blocking that matchmaking honours.
>
> **Constraints.** ⚠️ **Extend `LobbyChat`. There must never be a second chat system**, and it
> carries hard-won layout notes. Presence polling must not raise the service query rate: piggyback
> on the interval `ServerQuery` already runs. ⚠️ **A party of four is a full match, which is a
> ranked problem**: decide the rule in Phase 9 and assert it in a test there.
>
> **Done when** two accounts can befriend, see each other's presence, party up, queue together, and
> a block prevents a match, and § 0.5 rule 9 is satisfied.

---

### 19.7 Prompt for Phase 7

**Matchmaking, built on the lobby service that is already running.**

> Read `CLAUDE.md` first, then `docs/VISION.md`, then `docs/TODO.md`, then `docs/FUTURE.md` §§ 0.5
> and 0.6, then `docs/FUTURE.md` § 7 and `docs/INSPIRATION.md` § 3. Do not skip them because this
> prompt summarises the task; the summary is not the rules.
>
> **VERIFY FIRST.** Phases 1, 2 and 6 shipped, and `docs/INSPIRATION.md` § 3's queue structure has
> been built or at least decided, because it determines what queues exist.
>
> **Build.** QUICK MATCH as a rating-banded queue on top of the existing lobby integration, with no
> matchmaker service. Advertise a band in the lobby data, search for a joinable lobby whose band
> contains the local player, host one if there is none, widen the band on a timer, and **show the
> widening in the UI so a long queue reads as progress rather than as a hang**. Backfill an
> abandoned seat. Honour blocks. Separate pools by input device and platform.
>
> **Constraints.**
> - ⚠️⚠️ **The match-quality metric is the SPREAD of four ratings, not the gap between two
>   averages.** There is no team to balance here. A lobby with one 1400 and three 900s is a bad
>   match even though every team-based fairness formula calls it balanced. Put the metric in
>   `Packages/com.tumbangpreso.core/` with a test for exactly that case.
> - This must not raise the query rate against the free tier.
> - Say in the queue UI that the taya rotates and everyone defends once. It is why a bad first
>   round is not a lost match, and the game has never said it out loud.
>
> **Done when** four clients queue and land in one match, the band widens visibly, backfill works,
> and § 0.5 rule 9 is satisfied.

---

### 19.8 Prompt for Phase 8

**Competitive integrity. ⚠️⚠️ PHASE 9 MUST NOT START UNTIL 8.1 IS DONE.**

> Read `CLAUDE.md` including § 4 on architecture invariants, then `docs/VISION.md`, then
> `docs/TODO.md`, then `docs/FUTURE.md` §§ 0.5 and 0.6, then `docs/FUTURE.md` § 8. Do not skip them
> because this prompt summarises the task; the summary is not the rules.
>
> **VERIFY FIRST.** Phase 2 shipped. Confirm that every peer still receives every scoring event,
> because the witness design rests on the scoreboard being derivable on any peer
> with nothing new crossing the wire.
>
> **Build.**
> 1. **A witnessed result.** The host submits the final scoreboard, and ONE peer chosen at random
>    at match end submits its own independently derived copy. The endpoint accepts when the two
>    agree and flags the match when they do not. Two submissions, not four: § 8.1 records why the
>    four-peer unanimous version was simplified on 2026-08-31 and why a random witness catches the
>    same cheater.
> 2. Reporting from the end-of-match screen and the profile, with a reason.
> 3. Leaver penalties that **distinguish a leave from a disconnect** using the reconnect window
>    `LobbySession` already implements, or a player with bad internet is punished for their ISP.
> 4. Escalating queue cooldowns.
> 5. Rate limits on every write, because a free tier is a budget an abusive client can spend.
> 6. Sanity checks on submitted records: impossible scores, durations, rates.
>
> **Constraints.** ⚠️ **Do not change how scoring works.** `MatchDirector.AddScore` stays the
> single host-side writer during the match, per `CLAUDE.md` § 4. This is a second, independent
> derivation for verification only.
>
> **Done when** a match submits two agreeing scoreboards and is accepted, a deliberately altered
> submission is rejected and flagged, `docs/TODO.md` records **exactly what this scheme does and
> does not stop, including that it does not stop a host and its witness colluding**, and § 0.5 rule 9 is
> satisfied.

---

### 19.9 Prompt for Phase 9

**Ranked. ⚠️⚠️ DO NOT START THIS WITHOUT PHASE 8.1. A rank a host can award themselves is worse
than no rank, because it turns every good player's win into an accusation.**

> Read `CLAUDE.md` first, then `docs/VISION.md`, then `docs/TODO.md`, then `docs/FUTURE.md` §§ 0.5
> and 0.6, then `docs/FUTURE.md` § 9 and `docs/INSPIRATION.md` §§ 2.19 and 3.3. Do not skip them
> because this prompt summarises the task; the summary is not the rules.
>
> **VERIFY FIRST.** Phases 1, 2, 6, 7 and **8.1** all shipped. Confirm the corroboration endpoint
> exists and is the only writer of a result, because ratings go through it and nowhere else.
>
> **Build.**
> 1. ❌ **NOT two ladders. ONE rank.** This said Classic and Hero Strike each get their own
>    rating. Cut on 2026-08-31 on player-facing complexity: two ratings leaves a player with two
>    ranks and no answer to "what rank are you". **One competitive ladder, the other mode
>    unranked.** ⚠️ Which mode carries it is NOT decided; ask before building. § 9 has the entry.
> 2. **Glicko-2 adapted for a four-player free for all**: expand each result into six pairwise
>    outcomes, feed all six in, scale the step so one match moves a settled player about as much as
>    one game should.
> 4. Tiers with divisions and a numbered apex leaderboard. Names come from the game's own voice and
>    🧑 chooses them; § 9 has a suggested shape.
>    ⚠️ **WHETHER THE DIVISIONS SURVIVE IS AN OPEN QUESTION, ASKED AND NOT ANSWERED 2026-08-31.**
>    Five tiers times three divisions plus an apex is sixteen rungs of invented vocabulary before a
>    player knows whether they are any good. 🧑 was asked and chose neither way. **Ask before
>    building; do not assume the sixteen-rung version.**
> 5. ❌ **No placement matches**, cut on 2026-08-31: § 9. Start everyone mid-ladder with a wide
>    deviation and show the tier from match one. Glicko converges in the same handful of games.
> 6. A seasonal soft reset toward the mean, never a wipe, with a permanent peak on the profile.
> 7. ❌ **No demotion buffer and no score-margin multiplier**, both cut on 2026-08-31: § 9 says why. Do not add either back.
> 8. **Rank floors**, per `INSPIRATION.md` § 2.19: once a tier is reached the season cannot fall
>    below it. It costs one comparison and it removes the most common reason people stop queueing.
> 9. The party rule chosen in Phase 6, asserted in a test.
>
> **Constraints.** The whole rating model lives in `Packages/com.tumbangpreso.core/` with tests
> that assert convergence over a simulated season, that a new player settles inside ten
> matches from a mid-ladder start, and that a clearly stronger new account climbs out of a low band quickly. The Unity side
> is submission, display and the season boundary, nothing else. ⚠️ **No rank decay**: it punishes
> people with school and jobs, which is this entire audience. ⚠️ **Ranked changes stakes and
> integrity, never the rules in `docs/Design.md`.**
>
> **Done when** a simulated season converges, a new player settles inside ten matches from a mid-ladder start, ratings are written only by
> the Phase 8 endpoint, and § 0.5 rule 9 is satisfied.

---

### 19.10 Prompt for Phase 10

**Loadouts, skill variants and achievements. ⚠️ Read § 5.4 of `INSPIRATION.md` before writing a
line: this is the phase that can quietly turn the competitive mode into an account power check.**

> Read `CLAUDE.md` first, then `docs/VISION.md` § 1 twice, then `docs/TODO.md`, then
> `docs/FUTURE.md` §§ 0.5 and 0.6, then `docs/FUTURE.md` § 10, `docs/INSPIRATION.md` § 5 and
> `docs/Hero_Strike_Balance.md`. Do not skip them because this prompt summarises the task; the
> summary is not the rules.
>
> **VERIFY FIRST.** Phases 4 and 5 shipped. Confirm the ability budget assumptions in
> `docs/Hero_Strike_Balance.md` still describe the shipped kits, because every option below is
> defined as a trade at an unchanged budget and that only means something if the budget is real.
>
> **Build, in this order, because the first half has no balance risk and the second half does.**
> 1. Cosmetic and expressive mastery paths **for the six heroes only**: victory pose, character
>    emote, voice line set, nameplate, that hero's own tsinelas, a colour variant, a title, a
>    visible mastery number. **Most of the grind should live here.**
>    ⚠️⚠️ **THE SIX HEROES, NOT ALL EIGHTEEN CHARACTERS.** Narrowed 2026-08-31 on player-facing
>    complexity: eighteen paths is eighteen parallel grinds, and the twelve non-hero characters
>    have no kit to learn, so a path behind them is a grind attached to nothing. The other twelve
>    get a played count and nothing else. § 10 has the entry.
> 2. ❌ **NOT a Classic-only Street Hype track.** Cut on 2026-08-31: it is a second progression
>    system whose only reason to exist is which mode you picked, so the same match feeds a
>    different bar off a lobby toggle. **Street Hype stays an in-match feel in Classic** and earns
>    account XP like everything else. ⚠️ `VISION.md` § 1's rule that **Classic never gets powers**
>    is untouched and was never about Classic needing its own track.
> 3. The Hero Strike loadout: a pool of options per ability slot, chosen before the match.
> 4. Achievements in the three tiers in `INSPIRATION.md` § 5.6, each paying a title, a badge or a
>    banner tracker so nothing is a dead list.
>
> **The rules that make this safe. All four are load-bearing.**
> - ⚠️⚠️ **Every option is a sidegrade at an unchanged budget.** Nothing unlocks more damage, range,
>   duration or a shorter cooldown. **Write a test that fails if any option is a strict improvement
>   on its siblings along every axis.** That test is what keeps this honest three seasons from now
>   when somebody adds option four in a hurry.
> - ⚠️⚠️ **Every unlock challenge must be completable in Practice against bots**, and a test must
>   assert it for every challenge in the set. This is the rule that dissolves the competitive
>   problem: the gate then costs time learning a character, never matches won against people.
> - ⚠️ **The build is public**, shown in the lobby and on the scoreboard. Hidden loadouts in a
>   four-player fight are information asymmetry that feels like cheating.
> - ⚠️ **Do not build the swap-at-role-change idea in this pass.** `INSPIRATION.md` § 5.5 explains
>   why it is the most interesting idea here and also a real balance risk. Prototype it in custom
>   games afterwards and write the measurement into `docs/TODO.md` before it goes near ranked.
>
> **Constraints.** Option definitions, budget arithmetic and challenge conditions live in
> `Packages/com.tumbangpreso.core/`. Hero Strike only.
>
> **Done when** a build can be chosen, seen by opponents, and unlocked by a challenge completed
> against bots, both tests above exist and pass, and § 0.5 rule 9 is satisfied.

---

### 19.11 Prompt for Phase 11

**Bots, backfill and the population problem. Not glamorous, and it is the difference between a game
that lives and one that does not.**

> Read `CLAUDE.md` including § 7.1, then `docs/VISION.md`, then `docs/TODO.md` §§ 10 and 16, then
> `docs/FUTURE.md` §§ 0.5 and 0.6, then `docs/FUTURE.md` § 11. Do not skip them because this prompt
> summarises the task; the summary is not the rules.
>
> **VERIFY FIRST.** Read `docs/TODO.md` § 16 before tuning anything: it records that a single
> `BotBehaviourProbe` run spreads about 20 per cent, so one run is never a comparison.
>
> **Build.** Difficulty tiers for bots exposed in Practice and custom games. Bot backfill of an
> abandoned seat so a match continues rather than collapsing. Disclosed bot fill in the casual queue
> after a wait threshold, because a 45-second queue that ends in a playable match beats a
> four-minute queue that ends in nothing. ❌ No named practice ladder: cut on 2026-08-31, § 11.
>
> **Constraints.**
> - ⚠️⚠️ **Never bots in ranked. Not once, not to fill, not disclosed.** A test must assert it.
> - A match a bot joins becomes unranked from that moment, and the humans who stayed take reduced
>   rating loss.
> - ⚠️ **Label every bot visibly** in the scoreboard and the nameplate. A player who thinks they
>   beat a person and did not will be angrier when they find out.
> - Tune tiers across several probe runs, never one.
>
> **Done when** a seat that leaves is filled within seconds, the match is marked unranked, ranked
> refuses bots and a test proves it, and § 0.5 rule 9 is satisfied.

---

### 19.12 Prompt for Phase 12

**Custom games first, then modes, then map rotation. Every mode is cheaper once custom games
exist.**

> Read `CLAUDE.md` first, then `docs/VISION.md`, then `docs/Design.md`, then `docs/TODO.md`, then
> `docs/FUTURE.md` §§ 0.5 and 0.6, then `docs/FUTURE.md` § 12. Do not skip them because this prompt
> summarises the task; the summary is not the rules.
>
> **VERIFY FIRST.** Check what the map list actually is now rather than trusting § 0.2's count, and
> check whether custom lobbies already exist in any form before building a second path.
>
> **Build, in this order.**
> 1. **Custom games**: private lobby, password, round length, score target, character and tsinelas
>    restrictions, bot count, rule toggles. Everything else in this phase gets cheaper afterwards,
>    and it is also the tournament tool for Phase 17.
> 2. ⚠️ **Not the daily seed.** It was cut on 2026-08-31 and `INSPIRATION.md` § 2.9 records why.
> 3. Map rotation and a map vote. **Do these before building a fourth map**: voting buys most of
>    the same freshness for a fraction of the work.
>
> **Constraints.** Every new mode adds its rules to `Packages/com.tumbangpreso.core/`, never to
> Unity code. ⚠️ **A new mode is a new mode, never a change to Classic.** `docs/Design.md` governs
> Classic and `VISION.md` § 1 governs why. Write each mode's rules and win condition into
> `docs/Design.md` or a sibling document in the same commit as the code.
>
> **Done when** a private lobby can be created, configured, joined by code and played to
> completion, map voting works, and § 0.5 rule 9 is satisfied.

---

### 19.13 Prompt for Phase 13

**Seasons and the reason to open it on a Tuesday.**

> Read `CLAUDE.md` first, then `docs/VISION.md`, then `docs/TODO.md`, then `docs/FUTURE.md` §§ 0.5
> and 0.6, then `docs/FUTURE.md` § 13. Do not skip them because this prompt summarises the task;
> the summary is not the rules.
>
> **VERIFY FIRST.** Phases 2 and 4 shipped. If `INSPIRATION.md` prompt I3 has already built the
> challenge engine, **use it and do not build a second one.**
>
> **Build.** A ten-week season that exists for the ranked soft reset and its summary card, the
> weekly hour and Liga ng Barangay. Keep a live-ops calendar in the repo.
> ❌ **Cut, all on 2026-08-31, § 13:** daily challenges, **weekly challenges**, **login streaks**,
> the season theme, the season track and the featured-mode rotation. ⚠️ **There is no recurring
> challenge cadence left, and that is deliberate** rather than an omission to helpfully restore:
> a challenge list is a to-do list, and a streak's mechanism is making a missed day feel expensive.
>
> **Constraints.**
> - Every season boundary and reward is evaluated server-side from match records, never claimed by
>   a client.
> - Any event the calendar schedules is data in `Packages/com.tumbangpreso.core/`, not Unity code.
> - ⚠️ **If a challenge cadence is ever restored, bad challenges drive bad behaviour.** "Get 10
>   knockdowns" teaches a player to ignore the can, and the can is the whole game. Write them
>   against outcomes the game wants: retrievals under pressure, rounds survived as last attacker,
>   tags as taya, matches completed. **This is kept as the rule for a future decision, not as a
>   task.**
>
> **Read § 13.1 before anything else in this prompt.** It is the phase reasoned out rather than
> inherited, and it changes what "done" means: the four things that are left, the trap in the
> summary card, the population argument against a soft reset, and the rule that the calendar must
> work with the cable out.
>
> **Done when** a season boundary rolls correctly in a test, the summary card is built from the
> STORED profile at load rather than at submit (§ 13.1 item 1), a season with too few ranked
> accounts rolls over instead of resetting (§ 13.1 item 2), the weekly hour and Liga ng Barangay
> run off a core table with no service (§ 13.1 item 3), and § 0.5 rule 9 is satisfied.

---

### 19.14 Prompt for Phase 14

**Controller support. Starting point is zero: no gamepad bindings, no control schemes.**

> Read `CLAUDE.md` first, then `docs/VISION.md`, then `docs/TODO.md`, then `docs/FUTURE.md` §§ 0.5
> and 0.6, then `docs/FUTURE.md` § 14. Do not skip them because this prompt summarises the task;
> the summary is not the rules.
>
> **VERIFY FIRST.** `grep -c Gamepad Assets/TumbangPreso/Resources/TumbangPreso.inputactions`. If
> it is no longer zero, someone has started this; read what exists before adding to it.
>
> **Build.**
> 1. Keyboard and Mouse and Gamepad control schemes in the input asset.
> 2. Every gameplay and spectator action bound, including the spectator set that
>    `Rebinding.SpectatorContext` already names.
> 3. ⚠️ **The contextual `E` hold tiers**: tap picks up, a short hold shoves, a long hold as taya
>    resets the lata. A hold on a face button is fine; **the on-screen prompt is the hard part**,
>    because it names a key today.
> 4. Every prompt resolves its glyph from the last device used, not from a setting.
> 5. Rumble on knockdown, tag and can reset.
> 6. **Full menu navigation on a stick with no mouse**: character select, settings, lobby,
>    everything. This is a bigger job than the gameplay bindings, it is the thing that always gets
>    skipped, and skipping it blocks Phase 15.
>
> **Constraints.** Extend `Rebinding`; do not replace it. Keep the one-control-one-action-per-
> context rule that the existing input tests assert. ⚠️ **Do not add aim assist.** Record in
> `docs/TODO.md` that input-based matchmaking pools are the chosen answer: it is free, exact, and
> it removes the argument entirely.
>
> **Done when** a whole match and every menu can be played from a controller with the mouse
> unplugged, and § 0.5 rule 9 is satisfied.

---

### 19.15 Prompt for Phase 15

**Mobile. ⚠️ Be honest about the size of this: it is a port, not a feature, and it is the largest
item in this plan.**

> Read `CLAUDE.md` first, then `docs/VISION.md`, then `docs/TODO.md`, then `docs/TESTING.md`, then
> `docs/FUTURE.md` §§ 0.5 and 0.6, then `docs/FUTURE.md` § 15. Do not skip them because this prompt
> summarises the task; the summary is not the rules.
>
> **VERIFY FIRST.** Phase 14 shipped, because touch is built on the control-scheme work. Then list
> the installed build modules: if Android is not among them, installing it through Unity Hub is
> step zero and it is free.
>
> **Build, strictly in this order.**
> 1. **A build on a real device, however ugly.** Nothing else here means anything until that has
>    happened once. Do not polish before this.
> 2. Touch controls: left stick, look drag, buttons for throw, grab, jump, sprint, and a radial
>    long press with a fill for the contextual grab.
> 3. **A measured performance pass**, not a guessed one. The toon shader draws an inverted hull per
>    prop, which doubles draw calls on exactly the hardware least able to afford it. Measure it on
>    device, write the number into `docs/TODO.md`, then decide whether mobile drops the hull or gets
>    a cheaper one.
> 4. Phone aspect ratios added to the existing aspect-ratio probes rather than eyeballed.
> 5. Cross-play with separate pools, same reasoning as controller.
> 6. Battery, thermals, and a 30 FPS cap option.
>
> **Constraints.** ⚠️ **Keep the protocol version in lockstep with desktop.** Peers from different
> builds refuse each other by design, so shipping mobile and desktop at different versions will
> look like a bug and will not be one. **iOS is out of scope until there is a Mac to build on**:
> say that in the handoff rather than leaving it implied.
>
> **Done when** a full match can be played on an Android device against a desktop peer, the
> performance number is written down, and § 0.5 rule 9 is satisfied.

---

### 19.16 Prompt for Phase 16

**Accessibility. Cheaper now than at any later point, and the game has none of it.**

> Read `CLAUDE.md` first, then `docs/VISION.md` § 2, then `docs/Art_Direction.md` § 1, then
> `docs/TODO.md`, then `docs/FUTURE.md` §§ 0.5 and 0.6, then `docs/FUTURE.md` § 16. Do not skip
> them because this prompt summarises the task; the summary is not the rules.
>
> **VERIFY FIRST.** Confirm there is still no language setting and no colourblind option, and read
> `Art_Direction.md` § 1 in full, because the fix below has to hold its colour law for everybody who
> is not using an accessibility palette.
>
> **Build.**
> 1. ⚠️⚠️ **A second, non-colour channel for the taya-versus-attacker role, everywhere it is
>    currently hue alone.** A shape on the nameplate, an icon on the marker, a different outline
>    weight. Orange versus blue survives the most common colourblindness well and the roles being
>    distinguished by hue ALONE is the actual problem: it fails for tritanopia, on a bad projector
>    at a tournament, and on a cheap phone screen.
> 2. Then palettes for deuteranopia, protanopia and tritanopia that keep the two roles maximally
>    separated in what the player actually sees.
> 3. UI scale and larger text. Hold versus toggle for sprint and the contextual grab. An FOV
>    slider, a camera shake slider, and a reduced-effects mode that doubles as the low-end
>    performance mode. Subtitles and captions for callouts and ability sounds. A high-contrast HUD.
> 4. Extend the existing ability-showcase probe to **assert the reduced mode is measurably calmer**
>    than the default rather than assuming it.
> 5. ❌ **No localisation, and no string table.** English only, cut on 2026-08-31: § 16.3 records
>    why, and records the one thing to know if it is ever revisited.
>
> **Constraints.** Do not extract strings "while you are in there". § 16.3 is a decision, not an
> oversight.
>
> **Done when** the role is readable with hue removed entirely, the reduced mode is measurably
> calmer than the default, and § 0.5 rule 9 is satisfied.

---

### 19.17 Prompt for Phase 17

**Tournaments, LAN, spectating and replays. ⚠️⚠️ THE FIRST STEP OF THIS IS NOT FUTURE WORK.**

> Read `CLAUDE.md` first, then `docs/VISION.md`, then `docs/TODO.md`, then `docs/FUTURE.md` §§ 0.5
> and 0.6, then `docs/FUTURE.md` § 17. Do not skip them because this prompt summarises the task;
> the summary is not the rules.
>
> **VERIFY FIRST.** Read `LobbySession` and `LanBeacon` before writing anything: reconnect, seat
> reclamation and LAN discovery already exist and must not be rebuilt.
>
> **Step one, and do it before anything else in this prompt.** ⚠️⚠️ **Verify that a full
> four-player match can be started and completed with the internet physically disconnected**, and
> fix whatever screen fails. The nationals are in General Santos City and venue internet cannot be
> assumed. A tournament build that needs an online service to start a match is a build that can
> fail in the room. Add a test or a probe that keeps this true, because every later phase is a
> chance to put a login wall in front of it.
>
> **Then build.**
> 1. Tournament mode: lobby password, fixed roster, fixed map, no matchmaking, spectator slots that
>    do not consume a seat, organiser restart.
> 2. **Replays as the input stream plus the seed, not video.** The match is deterministic from the
>    input intent because a bot presses the same buttons a human does and one physics step serves
>    both, which makes a small replay possible instead of a huge one. ⚠️ **Prove determinism first**
>    with a test that replays a recorded match and asserts an identical final scoreboard. If it does
>    not reproduce, find the non-determinism and write it into `docs/TODO.md` before building
>    anything on top of it.
> 3. Clip export: save the last 30 seconds.
> 4. A caster overlay laid out for a stream rather than for a player.
> 5. A configurable spectator delay, so a stream cannot be sniped.
> 6. Highlight detection off events already raised.
> 7. An organiser's checklist document. ⚠️ **The thing that loses a tournament is a build
>    mismatch**: peers from different builds refuse each other by design, so every machine in the
>    room must be on the same executable and somebody must own that.
>
> **Constraints.** Extend `SpectatorCamera`; never write a second spectator path.
>
> **Done when** the offline check passes and is protected by a test, a tournament can be run
> end to end, a replay reproduces a scoreboard exactly, and § 0.5 rule 9 is satisfied.

---

### 19.18 Prompt for Phase 18

**Distribution. Last, and smaller than it looks.**

> Read `CLAUDE.md` first, then `docs/VISION.md`, then `docs/TODO.md`, then `docs/FUTURE.md` §§ 0.5
> and 0.6, then `docs/FUTURE.md` § 18. Do not skip them because this prompt summarises the task;
> the summary is not the rules.
>
> **VERIFY FIRST.** Confirm the WebGL module is still installed and that the game still builds for
> it, before promising a browser build to anybody.
>
> **Build.** Automate an itch.io release from a tagged commit through a GitHub Action. Get the
> WebGL target building and playable in a browser with the online path working. Write the store
> page from `docs/VISION.md`'s own words rather than inventing new ones.
>
> **Constraints.** ⚠️ **Name no vendor, engine, middleware or tooling in any public-facing
> material.** Write the capability, not who supplied it. This is a standing rule and it applies to
> the store page, the trailer description and the press kit.
>
> **Done when** a tag publishes a build without anybody touching a dashboard, the browser build
> plays a real match, and § 0.5 rule 9 is satisfied.
