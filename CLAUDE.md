# CLAUDE.md

The rules of this repository. Read this first, every session.

**Read order: this file, then [`docs/VISION.md`](docs/VISION.md), then
[`docs/TODO.md`](docs/TODO.md).** Everything else is reference;
[`docs/README.md`](docs/README.md) indexes it.

Every ⚠️ below replaced something that actually went wrong. None of them are style
preferences. If one looks removable, read the sentence after it.

---

## 1 · What this repository is

The **Unity 6 build** of Tumbang Preso, 1st place at the Gear Up NCR Esports Game Dev
Challenge and NCR's entry at the nationals in General Santos City.

⚠️⚠️ **THIS REPO IS THE GAME. THE GODOT REPO IS FROZEN REFERENCE FOR THE OLD VERSION.**
[DOST-GameDev](https://github.com/DOST-GameDEV/DOST-GameDev) may be read, quoted, ported from
and cited. **Never edit it, commit to it, or copy one of its files over the equivalent here.**
Where the two disagree about anything, including a design document, **this repo is current.**
Several sessions have re-derived this relationship and got it backwards.

⚠️ **The concrete trap:** `Design.md`, `Art_Direction.md` and `HUMAN.md` exist in both
repos. **The live ones are the copies in `docs/` HERE.** They were carried over from the Godot
repo and used to sit in a `docs/godot/` folder under a rule saying to edit them THERE and copy
them here; that rule inverted the day this repo became the game, and the folder name was then
telling every reader the opposite of the truth. Flattened into `docs/` on 2026-08-23.

⚠️⚠️ **THE GAME IS TWO MODES AND BOTH SHIP.** Classic is the street game with no powers, for
players who want less happening on screen. Hero Strike adds six heroes with two skills and an
ultimate each, and exists to raise the ceiling for competitive play. **Neither is a variant of
the other.** `docs/VISION.md` § 1 has the reasoning and the rules that follow.

⚠️ **`Port_Ledger.md` § 12 records an ability layer being DELETED, and that is not this.** The
deleted one was eight verbs bolted onto the single game. Hero Strike is a separate mode.

---

## 2 · How a session runs

### 2.1 Do the work. Do not narrate it.

⚠️⚠️ **DO NOT WRITE STATUS REPORTS. THAT IS WHAT STOPPING *IS*.** A turn ends the moment prose
is written instead of another tool call, so every "here is where things stand" summary IS the
stop. 🧑, after it happened repeatedly: *"i do not want to have to ask u to continue"*, *"why
do u even keep stopping"*, *"dont write a report then"*.

No mid-task progress summaries. Not "what is done so far", not "what is left", not "landed X,
next is Y". Report once, at the end.

⚠️⚠️ **NEVER CITE CAPACITY.** Not "context limits", not "what I can hold in one stretch". Do
not raise the subject at all. It was once claimed at well under half the window, so it was not
even true. It reads as an excuse for quitting on work that was explicitly asked for.

Also do not stop to ask whether to continue work already asked for, or for permission for a
step a standing instruction already covers (pushing, in particular).

**The only legitimate stops:** the work is genuinely finished, or something is truly blocked.
When blocked, finish everything that is NOT blocked first, then ask ONE specific question.

⚠️ **Do not editorialise about deadlines or whether something is achievable.** This team built
the whole game, which won its regional, in under two weeks.

⚠️ **Do not hand work back that you could do yourself.** Scenes can be built from code
(`Assets/TumbangPreso/Editor/SceneBuilder.cs`), matches can be run and measured headlessly,
and compilation, tests, probes, renders and builds all run from the command line. Hand back
only a human judgement ("does this FEEL right", "is this the art we want") or a credential.

### 2.1b ⚠️⚠️ NEVER SIT AND WATCH A TEST RUN. RUN IT IN THE BACKGROUND AND KEEP CODING.

🧑 2026-08-29, watching a fifteen-minute stretch of nothing but Unity launches: *"can u
make sure ur never wasting time js doing fucking tests, unless its the last thing, i hate it ...
code or some shit while ur doing tests"*.

A batchmode Unity launch is **three to twelve minutes** and a cold `Library` import is longer.
Blocking on one is that many minutes of a session spent producing nothing, and this repo's normal
day is a dozen of them.

**So:**

- **Start every `Unity.exe -runTests`, every `-executeMethod`, every build and every long probe
  with `run_in_background`.** The harness re-invokes when it finishes; there is nothing to poll
  and nothing to wait for.
- **Then immediately start the next piece of work.** Read the next file, write the next fix,
  draft the next `docs/TODO.md` entry. There is always something that does not depend on the
  result you are waiting for.
- **Collect the verdict later**, off `Logs/*.xml`, when the notification arrives. ⚠️ Still assert
  on the XML and never on the exit code (§ 7).
- **The one exception is the LAST run before a build**, where the verdict IS the next step and
  there is genuinely nothing else to do.

⚠️ **`dotnet test` ON `Core.Tests` IS NOT THIS.** It is about 40 ms and needs no editor, so run
it in the foreground as often as you like — it is the cheapest signal in the repo and the reason
the rules core is engine-free.

⚠️ **AND DO NOT EDIT `.cs` FILES WHILE A UNITY RUN IS IN FLIGHT.** It recompiles mid-run and
the result describes neither version. Edit documents, plan, or read while one is going; save the
code edits for when it lands, or start the run only once a batch is complete.

### 2.2 The shape of a session: WORK → BUILD → HANDOFF

🧑 2026-08-16: *"ALL TASKS I ASK -> build -> handoff"*, and, watching a build start with six
items still open: *"ur supposed to build only when ur done with everything"*.

1. **Do ALL the work** and verify it yourself: `dotnet test`, EditMode, PlayMode, and the
   probes for anything a screenshot would settle.
2. **Build the .exe to the Desktop.** ⚠️⚠️ **DELETE THE PREVIOUS BUILD FIRST, EVERY TIME.**
   Not only when asked for a "clean rebuild", not only when a timestamp looks wrong: **every
   build deletes the old output folder before it writes the new one.** An incremental rebuild
   once kept a corrupted `level1` and cost an hour, and Unity will rewrite `TumbangPreso_Data`
   while reusing the byte-identical launcher, so the finished .exe still carries the OLD
   creation timestamp and looks stale. `GameBuilder.PurgeOutputDirectory` now does this in code
   rather than trusting anyone to remember it, and it refuses to delete a path that is not
   obviously a previous player. § 7 has the procedure for the manual case.
3. **Write the handoff in the chat reply.** Never as a file. See § 2.4.

⚠️⚠️ **"TASK" MEANS THE WHOLE REQUEST, NOT EACH ITEM IN IT.** A build is a claim that there is
something worth looking at. If any item he raised is still unfinished, keep working: do not
build, and do not come back.

⚠️ **PUSH AUTOMATICALLY. FINISHED MEANS PUSHED.** Committed and waiting is not done. Every
batch that compiles and passes goes up without being asked.

### 2.3 `docs/TODO.md` is the shared worklist. Tick it and add to it.

⚠️⚠️ **EVERY SESSION READS IT, EVERY SESSION UPDATES IT, IN THE SAME COMMIT AS THE WORK.** It
is how work survives being handed between sessions and tools. Specifically:

- **Check it before inventing a task.** The thing you are about to do may already be written
  up with its cause and its acceptance test.
- **Tick items off as you finish them.** Move them to **Closed** with one line on how it was
  verified, not just "done".
- **Add anything you find and do not fix.** A bug noticed and not written down is a bug
  rediscovered from scratch by somebody else in three weeks. Give it the same shape as the
  other entries: what is wrong, where it lives, what done looks like.
- **Keep the numbers.** An entry that says "40% of the arena" beats one that says "too big",
  because the next person can act on the first without re-measuring.

### 2.4 The handoff contract

⚠️⚠️ **NEVER COMMIT A HANDOFF PROMPT AS A FILE.** It goes in the chat reply, to be
copy-pasted. A stale one committed to a repo has already had to be deleted twice.

⚠️⚠️ **AND EVERY HANDOFF MUST POINT THE NEXT SESSION AT THE RULES.** A handoff that only
describes the work produces a session that starts editing without knowing the repo is live,
that both modes ship, or that pushing is automatic. **Open every handoff with this, adapted:**

> Read `CLAUDE.md` first, then `docs/VISION.md`, then `docs/TODO.md`. They carry the rules of
> the repo, what the game is for, and what is open. Do not skip them because this prompt
> summarises the task; the summary is not the rules.

Then the rest: repo and branch, the exact HEAD, current test and build state, what changed and
what was measured, and what to pick up next **with a pointer to its `docs/TODO.md` entry
rather than a copy of it.** Copies go stale; the file does not.

---

## 3 · Writing, commits and comments

⚠️ **NEVER add a `Co-Authored-By` trailer, of any kind.** This is sole-authored work entered
in a competition. Not "just this once", not because a template suggests it.

⚠️ **Never mention Claude, Anthropic, or any AI tooling** in commit messages, code comments,
the README, or anything else in this repository.

⚠️ **No em dashes anywhere.** Rewrite the sentence rather than swapping the character in.

⚠️ **PowerShell here-strings break on embedded double quotes** when passed to `git commit -m`.
Write the message to a file and use `git commit -F`.

**Comment the WHY, at length, in ⚠️-marked notes above the thing.** Record deletions and the
reasoning, not just the change. A number that was measured says so, and says what it was
measured against. This is the Godot codebase's discipline and it is why this port was
tractable.

---

## 4 · Architecture invariants

Do not "improve" any of these. Every one replaced something that failed in play or in a probe.

⚠️⚠️ **`Packages/com.tumbangpreso.core/` must never acquire a `UnityEngine` reference.** It
holds the match rules, scoring, trait tables, the stamina model, throw legality and the combat
geometry: every number arrived at by measurement rather than taste. Engine-free is what lets
them be asserted in a second instead of playtested for an afternoon. The asmdef enforces it
with `noEngineReferences`; do not add an exception.

⚠️ **The source lives in the package and `Core/TumbangPreso.Core.csproj` compiles those same
files in place.** One copy, two toolchains. Never "fix" this by copying: the copy that drifts
is the one nobody runs the tests against.

- **Contact resolves by DISTANCE on the host, never by a trigger volume.** 16 of 36 overlaps
  were measured failing to land. It also keeps the correctness-critical code free of the
  physics engine.
- **Every point is awarded in ONE function**, host-side (`MatchDirector.AddScore`). A point
  that can only be created in one place cannot be created on a client at all.
- **The taya role is DERIVED**, `(round - 1) % 4`, never accumulated.
- **The box is a SQUARE, not a circle**, and X and Z clamp independently. They disagree by
  2.9 m on the diagonal, which is exactly where a taya moves to cover a corner.
- **A bot presses the same buttons a human does** (`InputIntent`). One physics step serves
  both. Never let AI call a gameplay method directly.
- **Stuns overlap via `Max()`, never additively.** That is the entire bound on a stun chain in
  a 1-vs-3 game.
- **Every impulse is derived from `Friction`** as `v²/(2·Friction)`. Write the distance you
  want and solve for the speed; never hard-code a distance beside a speed.
- **Entry 0 of each prop list stays neutral.** It is what an unpicked prop wears.
- **One control, one action, in the input map.** ⚠️ **Per CONTEXT, since 2026-08-27.** The panel
  refuses a key another action *in the same context* holds, so shipped defaults that break that
  rule are a defect. `InputMapAndAbilityTests` asserts it.
  ⚠️⚠️ **THE SECOND CONTEXT IS SPECTATING AND IT IS A NARROWING OF THIS RULE, SO READ WHY BEFORE
  WIDENING IT AGAIN.** Nine spectator controls (TAB, F, V, B, N, P, R, C and the new autopilot
  key) were `Keyboard.current` reads outside the input asset entirely until then: not rebindable,
  not visible in the panel, not checked by anything. Four of them reuse keys gameplay actions
  hold. **A spectator has no body, no seat and no `CharacterMotor`**, so while watching every
  gameplay action is inert and while playing none of the spectator set is reachable: they can
  never both fire, which is the only thing this rule was ever protecting.
  `Rebinding.SpectatorContext` names the set, `SpectatorBindingTests` asserts the narrowing from
  both sides, and `docs/TODO.md` § 35.3 has the reasoning. **Two actions inside one context
  sharing a key is still a defect.**

⚠️ **The camera is FPP *and* TPP. Do not "simplify" it to one.** A Person is always FPP, a
Prop is always TPP, derived from `is_person` and asserted. Emotes swing to TPP and back,
orbiting the body, local-only. Spectator is a fourth rig entirely (`spectator_camera.gd`:
free, follow, POV). An earlier session recorded "third person was a mistake"; **that note was
wrong** and acting on it would delete three shipped features. The genuine earlier mistake was
narrower: an *overhead follow* camera that matched none of the four.

⚠️ **Emotes end ONLY by interruption.** 🧑: *"it doesnt end on its own"*. There is no emote
timer and no clip-finished stop. `EmotePlayer.Stop()` is reached by movement, a verb, or
losing the right to act, and that is the single path the camera's `EndEmoteView` hangs off. If
a clip-finished path is ever wanted, route it through `Stop()`.

---

## 5 · Design.md and the ledger

**`docs/Design.md` is the balance source of truth.** It opens with: *a number in the
code must match a number here, or one of the two is a bug.*

✅ The eight places it had drifted are corrected as of 2026-08-23, and every one was stale
prose rather than a code defect. `docs/Design_Drift_Report.md` holds the evidence.

⚠️ **The habit that produced the drift has not changed: port from the GDScript and from
`Balance.cs`, never from the prose.** If you find a ninth disagreement, fix the prose, note it
in the drift report, and say so in the commit rather than silently picking a side.

⚠️ **`Design.md` describes Classic only.** Hero Strike, the ability kits and the ultimate
charge have no entry in it; its § 13 lists what it does not govern and points at the files
that do.

**`docs/Port_Ledger.md` is the definition of done** for the port: every Godot script and scene
with a CONVERTED / PARTIAL / MISSING status, measured from both trees. Update the row when you
finish something. Do not report the port as done while any row reads MISSING. A 26-line
`kill_plane.gd` is still a feature the player meets.

---

## 6 · Art, models and iteration

⚠️ **The art is the team's own work and is being built character by character.** The voxel
cast is authored here (`tools/build_person_voxel.py`, `docs/Voxel_Person_Guide.md`), and
`docs/Port_Plan.md` § 8 carries the replacement queue and what each replacement must preserve.
Ask which pieces are final before treating any of it as disposable.

⚠️ **When new animations land, revisit `ModelImportSetup`.** The rigs are imported as
**Generic** on purpose, because the current clips ship with their own rig; humanoid
retargeting would re-solve poses that are already correct. If clips start coming from a
library instead (Mixamo or similar), **Humanoid becomes the right answer**, and that is the
single biggest thing Unity buys over Godot here.

⚠️ **The IKE slipper carries the real Nike wordmark as geometry.** First in the replacement
queue; `docs/Port_Plan.md` § 8 lists the properties a replacement must preserve.

### 6.1 Every model iteration gets a picture, and every picture gets a new filename

⚠️⚠️ **SHOW, DO NOT DESCRIBE. A model change with no render attached cannot be judged**, and
describing a mesh in prose is the slowest possible way to be told it is wrong.

- **Render after every iteration**, through the in-engine probe pipeline. Never an external
  renderer or a software OpenGL preview: the toon shader, the ink outline and Unity's linear
  colour conversion are the look, and anything outside the engine misses all three.
- **The two canonical outputs** are the **4-angle turnaround** and the **cast lineup**, which
  is what shows the character next to the rest of the roster. Orbit renders are deprecated.
- ⚠️⚠️ **VERSION THE FILENAME EVERY TIME: `zack_hair_v1.png`, `zack_hair_v2.png`.** Chat
  clients cache images by filename, so overwriting a render leaves the previous one on screen
  and the whole review is conducted against an image that no longer exists on disk. This has
  happened and it wastes a full round trip.
- **Force-reimport sub-assets before rendering.** Rebuilding a pet or accessory `.glb` from
  Python changes the file on disk while Unity keeps the old one in memory; the render then
  shows geometry that is no longer there.

### 6.2 Every screen gets designed, and the method is written down

⚠️⚠️ **A FEATURE WITHOUT A SCREEN IS NOT SHIPPED, AND "I ADDED A ROW FOR IT" IS NOT A DESIGN.**
🧑 has rejected the same screen twice for the same reason: *"theres liek 20 shits at once"*
(`docs/TODO.md` § 92) and then *"its so messy and ugly"*, *"I js wannted u to imrpove hwo u put
the text annd readability and visual hierarachy"* (§ 94.7). Both were built by the last person to
touch the feature, at the end, without a method.

**The method is [`docs/FUTURE.md`](docs/FUTURE.md) § 0.5b and it applies to ANY screen in this
repository**, not only to the phases in that file. Five questions before you write it, four
ordering tools in order (position, size, weight and colour, space), a table of what actually
transfers from the games it copies, and the four things a screen owes before it is done.
⚠️ **§ 0.5b's per-phase table answers "what is the one thing on this screen" for every remaining
phase**, so no screen starts from a blank page.

⚠️⚠️ **AND THE LINE WORTH REPEATING HERE: A GREEN LAYOUT PROBE IS NOT A GOOD SCREEN.**
`PlayerHubLayoutProbe` and `PhaseSurfaceLayoutProbe` assert every label fits its box and clears
the 18-unit floor. **Seven readability faults were true at once while both were green**, including
a value drawn 1600 px from its label and an XP bar drawn underneath a button. **The probe asks
whether the screen is a screen; the picture asks whether it can be read.** Take the picture.

### 6.2b ⚠️⚠️ THE FOUR WAYS A SCREEN SHIPS BROKEN AFTER BEING "RENDERED", AND THE CHECK FOR EACH

🧑, after opening a build whose new boot screen drew as a floating form over a fully lit menu with
the nameplate across it: *"i opened the game what the fuclk is this"*, then *"theres problems like
this that i want to be checked nnext time UI is made"*, *"I want the user version to actually be a
great experience"*.

**That screen HAD a render. Four of them, green, at nine resolutions.** Every one was of a
different screen than the one he opened. **Take the picture is not enough; this is what to take a
picture OF.**

| Ask | Why it is not optional | What it cost |
|---|---|---|
| ⚠️⚠️ **EVERY STATE, not the one you built first.** | A screen with a mode has two layouts and you have looked at one. | The sign-in screen was shot only as `Open()`. It ships as `OpenAtBoot()` too, which hides BACK, renames a button and has no hub behind it. **The state a player meets first was the state nobody had seen.** |
| ⚠️⚠️ **OVER THE REAL BACKGROUND, never an empty scene.** | Every scrim, every panel alpha and every band is a number tuned against what is behind it. | `UiRows.Band` is 3.5 per cent measured against the lit street. Shots taken over a blank scene are shots of a different screen, and 🧑 spotted the swap instantly: *"i lowk liked the light brown bg earlier fuck that blue shti"*. |
| ⚠️⚠️ **AT THE SHAPE HE ACTUALLY PLAYS AT.** | `Fullscreen` is **false** in his `settings.json`. He plays in a short wide window, and all nine probe resolutions are taller than it. | A column of hard-coded Y offsets collapsed into a heap in the middle of the screen. **A screen that only exists at 16:9 is a screen nobody in this room has seen.** |
| ⚠️⚠️ **WITH EVERY ALWAYS-ON PIECE OF CHROME STILL LIVE.** | Chrome does not know about a screen added after it. | `PlayerNameplate` hides for the hub and for every `ConvertedOverlay` and knew nothing about a third code-built canvas, so it drew straight across the account form. **This is the third time that method has had to be taught about a new screen**, which is the argument for asking "is anything on top of me" rather than keeping a list. |

⚠️ **AND IF IT CANNOT BE RENDERED, IT DOES NOT SHIP OPEN.** A screen that appears unasked at boot
is the one screen where "I could not get a picture of it" is not an acceptable answer, because
every player meets it before anything else.

### 6.2c ⚠️⚠️ FOUR QUESTIONS ABOUT EVERY RECTANGLE ON A SCREEN, BECAUSE PHASES 1 TO 4 GOT THEM WRONG SEVEN TIMES

🧑, after four phases of account and career UI: *"phase 1-4 had horrible ui integraitons"*, and, on
the boot screen specifically: *"This shhit is horrible bro the art is cut off... u properly thinnk
abt how to make the characters look good"*.

**§ 6.2b is about photographing the right screen. This is about the screen itself, and every row
below is a fault that shipped in this repository rather than a principle.** `docs/TODO.md` § 92
(the six-button panel), § 94.7 (seven readability faults at once, all green) and § 100 (the art
fitted to a frame nobody can see) are the receipts.

| Ask, of every rect you write | The rule | What it cost |
|---|---|---|
| ⚠️⚠️ **What is this size measured AGAINST?** | **A size is only correct against the rectangle the player actually sees.** A percentage of the window is not a size: `AspectSafeCanvas` scales on the SHORT axis, so the canvas is about 1920 units wide at 4:3 and about 2250 on his window, and one fraction is two very different widths. **Size a panel against its CONTENT and state the arithmetic.** | § 100: the sign-in column was 38 per cent of the window around a 420-unit form, so on the window he plays in it was **860 units of wood around a form that never grew**. It is 580 units now, which is the form plus one margin either side, and it cannot swallow a narrow screen because `Expand` guarantees the canvas is never under 1920 units wide. Same family as § 92.1 fault 3, which is why `UiRows` takes no offsets. |
| ⚠️⚠️ **Is this image fitted to the region it is SEEN in, or to the whole screen?** | **Every image gets an explicit fit decision and an explicit parent.** Envelope a background, fit a logo, and in both cases the parent must be the visible region, not `_root`. If something opaque covers part of the screen, the picture's frame ends where that thing starts. | § 100: the key art enveloped the full canvas and the column then covered a third of it, so the crop was computed for a frame that does not exist and the cast came out off-centre with its heads cut off. `SignInScreen.BuildLogo` records the same fault one size down: `FitInParent` sizes against the PARENT, so a fitter with no box of its own drew the wordmark three hundred pixels tall through the form. |
| ⚠️⚠️ **What is this dimming layer FOR, and is that still true?** | **A scrim buys legibility over a live 3D scene, or separation from one. It is not decoration and it is not free.** If every word on the screen sits on an opaque panel, a scrim over the art side is dimming the one thing the player is meant to look at in exchange for nothing. **Ask what it protects before retuning it.** | § 100: 72 per cent over the live street, retuned to 55 when the key art landed, and never asked what it was still for. 🧑: *"nno nneed to darkenn it"*. `UiRows.Band` is the same rule the other way round: a number tuned against one background is not a number. |
| ⚠️⚠️ **If I delete this, what else was it doing?** | **Anything covering the screen is also eating clicks, and the block is usually nobody's stated job.** When a full-screen graphic goes, name its replacement blocker in the same commit. | § 100: the scrim was silently what stopped a press on the art side reaching the title screen underneath. Deleting it would have let a player press PLAY **through** the boot screen, on the one screen that exists to ask a question first. The key art is the blocker now, and it says so. |

⚠️⚠️ **AND NONE OF THE FOUR IS VISIBLE TO ANY PROBE IN THIS REPOSITORY, WHICH IS THE POINT OF
PUTTING THEM HERE.** `PlayerHubLayoutProbe` was green through every one of them, because a label
that fits its box fits its box whether the picture behind it is beautiful or butchered. **The probe
asks whether the screen is a screen. This section is what to look at in the picture.**

### 6.3 ⚠️⚠️ MOVING AROUND THE GAME IS ITS OWN DESIGN PROBLEM, AND THE UNIT IS THE JOURNEY

### 6.4 ⚠️⚠️ NEVER USE BLUE / NAVY OUTLINES OR COLD STROKES FOR UI ICONS OR ASSETS

🧑 2026-08-31: *"i dont like blue outlines its out of theme"*, *"can u put in claude md to never use blue outlines and shit for ui"*.

- **The rule:** UI icons, rank emblems, state badges, glyphs, and panels must **NEVER** carry dark blue, navy, or cold ink outlines. Outlines on brown wooden panels read as blue rings and clash violently with the Filipino street warm aesthetic.
- **The palette:** Hand-painted carved wood (`#31190B` deep wood, `#5A2F14` mid wood, `#8B5227` wood edge), warm cream paper/chalk inlays (`#F5E6C8`), and glowing amber gold (`#FFBA00`). Geometry is defined by warm tone-on-tone wooden bevels and borderless shapes, **NEVER** blue/navy outlines.

🧑, 2026-08-31: *"i want the user experience of movinng around the game to feel intuitive"*, and
*"lets say im a player and i want to do something or find something, make sure that entire
experience feels great"*.

**Walk the journey out loud before building any of it**: *"I want to X"* to *"X is done"*, naming
every press. If it takes more than three, or if one of them is a control the player has to
discover rather than read, **the flow is the bug and no amount of layout fixes it.**

- ⚠️⚠️ **EVERY DESTINATION HAS A VISIBLE DOOR, AND A DOOR IS A THING THAT LOOKS PRESSABLE.**
  `docs/TODO.md` § 96: the player hub had exactly one door, a corner chip stating a name and a
  level, and **the person who commissioned the hub never found it.** Four tabs, a career, a match
  history and the whole account system sat behind something that read as a status readout.
- ⚠️⚠️ **NEVER ADD A SECOND DOOR TO FIX A FINDABILITY PROBLEM.** That is exactly how § 92's
  six-button panel happened: a button per feature, each in its own visual language, each at its
  own hard-coded offset, and 🧑 asking *"look wtf why are these buttons here"*. **Fix the door or
  move it.**
- **Escape backs out on every screen, always, innermost layer first.** `ConvertedScreen.CancelTarget`
  exists because three screens shipped with a dead Escape; the hub and the sign-in screen are
  built in code rather than converted and inherited none of it until 2026-08-31. **A player who
  learns Escape is reliable and then meets one screen where it is not has learned that it is
  unreliable.**
- **A control that does something must react to the pointer; one that does nothing must not look
  pressable.** The pennants scale and light up and the plate beside them did not move at all.
- **A dead end is a bug.** A button that dismisses to nothing is worse than no button.
- ⚠️ **The escape from any gate is ONE press and never needs the network.** § 97, and the
  nationals in General Santos City are why.

⚠️⚠️ **`UiClickProbe` CAN PROVE NOTHING IS COVERED AND HAS CAUGHT NEW CHROME BLOCKING A SCREEN
THREE TIMES. IT CANNOT TELL YOU A DOOR NOBODY LOOKS AT IS A DOOR NOBODY FINDS.** That one needs a
person. Watch a launch, or ask what they expected to press.

⚠️ **`UiRows` OR IT IS NOT A SETTINGS-SHAPED SCREEN.** Nothing in that file takes an offset,
which is fault 3 of § 92.1 made impossible rather than fixed. A hand-written Y offset is a layout
correct at exactly one panel height and one aspect ratio, and `AspectRatioProbes` drives nine.

`docs/CANONICAL_RENDERING_PIPELINE.md` has the exact commands and five recorded pitfalls.
⚠️ **That document is written for Antigravity and its "MANDATE FOR ALL AGENTS" heading is that
tool's, not this one's.** Its render pipeline is correct and worth following; where anything
in it disagrees with this file, this file wins here.

---

## 7 · This machine

| | |
|---|---|
| Unity | `C:\Program Files\Unity\Hub\Editor\6000.5.8f1\Editor\Unity.exe` |
| Modules | Windows Standalone, WebGL, Linux Dedicated Server |
| dotnet | `C:\Program Files\dotnet\dotnet.exe`, SDK 9.0.317 |
| RAM | 16 GB. It read 8 GB until a boot-time cap was cleared, so re-check before blaming Unity |

```bash
dotnet test Core.Tests/TumbangPreso.Core.Tests.csproj
```

```bash
"/c/Program Files/Unity/Hub/Editor/6000.5.8f1/Editor/Unity.exe" -batchmode -runTests -nographics -projectPath . -testPlatform EditMode -testResults Logs/tests.xml -logFile Logs/tests.log
```

```bash
"/c/Program Files/Unity/Hub/Editor/6000.5.8f1/Editor/Unity.exe" -batchmode -runTests -projectPath . -testPlatform PlayMode -testCategory "!WallClock" -testResults Logs/play.xml -logFile Logs/play.log
```

⚠️⚠️ **`-testCategory "!WallClock"` IS PART OF THE COMMAND, NOT AN OPTIMISATION.**
`AiDiagnosticProbe` runs a round at 1x for about 80 real seconds by design, so its result depends
on how busy the machine is: it has failed at 21.6 s, 29.9 s and 37.6 s against a 20.0 s bound and
passed on immediate re-runs with nothing changed. `docs/TODO.md` § 6 carries the decision.
⚠️ `[Explicit]` does NOT do this in batch mode; it was tried and the tests still ran.
Run them on purpose with `-testCategory "WallClock"`.

**All five editor checks, in one launch:**

```bash
"/c/Program Files/Unity/Hub/Editor/6000.5.8f1/Editor/Unity.exe" -batchmode -projectPath . -executeMethod TumbangPreso.EditorTools.Checks.RunAll -logFile Logs/checks.log
```

```bash
"/c/Program Files/Unity/Hub/Editor/6000.5.8f1/Editor/Unity.exe" -batchmode -quit -projectPath . -executeMethod TumbangPreso.EditorTools.GameBuilder.BuildWindows -logFile Logs/build.log
```

⚠️⚠️ **PlayMode has NO `-nographics`, and adding it CRASHES the editor**, not the tests. Unity
selects `NullGfxDevice` and the first offscreen camera dies inside it. The run writes NO `.xml`
and **still exits 0**.

⚠️⚠️ **Always assert on the `.xml`, never on the exit code.** Both that crash and a genuine
failure come back as 0.

⚠️ **`-batchmode -quit` exits before compiling scripts** and still returns 0. Use
`-executeMethod` or `-runTests` when you need an actual compile.

⚠️ **Launch Unity with `Start-Process -Wait -PassThru`**, not `&`. With `&`, `$LASTEXITCODE`
comes back empty and the log file is sometimes never created, which is indistinguishable from
a failed run.

⚠️⚠️ **Unity leaves child processes holding the project lock after it exits, AND A STALE
`Temp/UnityLockfile` LOOKS EXACTLY LIKE A BROKEN INSTALL.** `Unity.ILPP.Runner`,
`UnityPackageManager` and `UnityShaderCompiler` can outlive the editor, and while they do the
next launch silently does nothing: no log, no error, no exit code.

**On 2026-08-26 a stale lockfile with no Unity process alive made the package manager answer
`path ... Received undefined` on every launch, including against an empty project.** The session
before it concluded Unity was broken machine-wide, gave up on EditMode, PlayMode and every build,
and handed that on as a blocker. `rm Temp/UnityLockfile` fixed it outright. **Check that file
before believing anything worse**, and check it whether or not a Unity process is running.

⚠️ **Bash heredocs are unreliable here.** Write the script to a file and run it.

⚠⚠ **REVERSED ON 2026-08-28: THE CORNER SHOWS THE VERSION AGAIN, ON EVERY BRANCH.** 🧑, pointing
at a corner reading `integration/ui-batch-on-ilalim`: *"pls replace the version number to 1.00"*,
*"instead of this"*. The game is at **1.00** now rather than mid-port, and the number is what goes
into a screenshot to a sponsor and what a player quotes in a report. `GameVersion.DisplayString`
is the ONE line that decides, and the machinery below is kept rather than deleted, so returning to
the branch name is a one-line change. **The reason it was built is real, and the paragraph below
is why: if two .exe files ever become indistinguishable again, put it back rather than rebuilding
it.**

The original rule, retained for that reason:

⚠️ **THE BOTTOM-RIGHT CORNER SHOWED THE BRANCH NAME, NOT THE VERSION, ON EVERY BRANCH BUT
`main`.** 🧑, 2026-08-27: *"for every branch made it would replace the version number on the
bottom right corner with the branch name instead"*. A build off
`fix/multiplayer-fpp-camera-inside-head` reads exactly that in the corner of every screen and of
the HUD; a build off `main` still reads `v4.72`.

**It is automatic and there is nothing to do by hand.** `GameBuilder.StampBuildBranch` writes the
checked-out branch into `Assets/TumbangPreso/Resources/BuildBranch.txt` on EVERY build, because a
player has no git; `BuildBranch` reads it, `GameVersion.DisplayString` picks between the name and
the number, and both corner labels go through `GameVersion.ApplyTo`. In the editor git is read
live, so play mode shows the branch you are actually on rather than the last one you built.

⚠️ **The stamp file is gitignored on purpose.** It changes on every build and per branch, so
committing it is a one-line diff per build and a conflict per merge, over a file whose whole job
is to be regenerated. Absent or empty both mean "show the version".

⚠⚠ **DO NOT PUT THE BRANCH NAME ON THE WIRE.** `Application.version` still carries the real
version into the LAN beacon payload, the online lobby record and the connection-approval hello,
and those are compared between peers: a name there would refuse two players built from the same
commit on different branches. `BuildBranch` is the LABEL and nothing else, and
`TheBranchNameNeverReachesTheVersionTheWireCompares` asserts the separation.

⚠️ **The label is sized against the string.** The authored rect is 132 px, which fits `v4.72`
and cuts `claude/multiplayer-lobby-switching-bugs-d1546c` in half; legacy `Text` defaults to WRAP,
so that overflow is silent. `ApplyTo` widens the box and switches to Overflow. This is the same
trap `ConvertedScreen.SetHeadline` records, for the third time.

⚠️ **`GameBuilder.BuildWindows` targets THIS MACHINE'S DESKTOP, whatever it is.** It calls
`Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)`, so it resolves per
profile: `C:\Users\matth\Desktop` on one laptop and `C:\Users\Matthew\Desktop` on the other.
⚠️⚠️ **THIS LINE USED TO NAME ONE OF THEM AS A CONSTANT**, which sent a session on the
other machine to check a folder that does not exist and reads exactly like a build that never
ran. Verify the .exe exists and report its path; do not claim a build that was never written.

⚠️⚠️ **A successful incremental Windows build is not the same as a clean rebuild.** Unity can
rewrite `TumbangPreso_Data`, Burst output and DLLs while reusing the byte-identical launcher
`TumbangPreso.exe`; Explorer then keeps the executable's old creation/modified timestamp. This
caused a build completed at 15:03 to look like the 14:34 player was still being shipped.

✅ **`GameBuilder.PurgeOutputDirectory` now deletes the output folder on EVERY build**, so the
old-folder half of this is automatic and step 2 below is only needed when you want the previous
player kept. The build **fails** rather than half-overwriting if the folder is locked, which is
almost always the game still being open.

When the user asks to **rebuild**, **clean rebuild**, or questions the output timestamp:

1. Ensure no `TumbangPreso` or Unity process is using the output. This is the one step still on
   you: a running game holds the folder and the build will refuse it.
2. To KEEP the old player, move `Desktop\TumbangPreso-Unity` to a clearly named backup first.
   Otherwise the build deletes it.
3. Run `GameBuilder.BuildWindows`. It writes into a now-missing directory by construction.
4. Verify the new `TumbangPreso.exe` **and** `TumbangPreso_Data` files have timestamps from the
   current run, then launch that exact executable. Keep the backup until the new player passes.

The build log's `SUCCEEDED` line proves Unity completed a player build; it does **not** prove that
every file in a pre-existing output directory was freshly emitted.

### 7.1 Verify by measuring

- `Core.Tests` asserts every balance number in about a second.
- `BotBehaviourProbe` runs a whole match in both modes and prints throws, retrievals, tags,
  skills, ultimates, penalties, EMOTES and HOPS, on Eskinita and on Ilalim ng Tulay. ⚠️ **It is seeded. Do not
  change the seed to make a run pass**; if a run goes red, change the code.
  ⚠️⚠️ **AND ITS NUMBERS ARE LIVENESS FLOORS, NEVER COMPARISONS AT n = 1.** It is stepped at a
  fixed 1/60 s now rather than at a 6x time scale, and that removed most of the noise and not all
  of it: eight matches at the shipped settings spread from **58 to 100 throws**, about 20 per
  cent, and two runs of one build with one seed are still not identical.
  ⚠️⚠️ **`docs/TODO.md` § 10 SAYS THIS WAS SOLVED AND § 16 IS THE MEASUREMENT THAT SAYS IT WAS
  NOT.** § 10 was closed on the ARGUMENT that a fixed step removes the clock; the first sweep to
  run one configuration twice got 43 throws and then 83. Read § 16 before quoting any number this
  probe prints as a comparison: it carries the noise floor and the arithmetic for how many runs an
  arm an A/B has to buy (three for anything worth 20 per cent).
  ⚠️ **`TwoIdenticalMatchesLandInsideTheNoiseFloor` is how you ask whether it is still honest**,
  six minutes, `WallClock`. Run it after touching anything the bots read.
  ⚠️ **Every report from before 2026-08-26 is three seats' worth**, because `GameLaunch.SoloSeat`
  defaults to 1 and that seat was a parked human until `GameLaunch.AllBots` landed. Do not compare
  an old report against a new one.
  ⚠️⚠️ **AND EVERY REPORT FROM BEFORE 2026-08-27 HAS SEAT 0 STEERING IN A ROTATED FRAME**, which is
  the second layer of that same fault and is `docs/TODO.md` § 34. `MatchInstaller` turned the
  gameplay rig off on `GameLaunch.Spectator` alone, so under `AllBots` it stayed active, followed
  seat 0 and kept `AimSource.Mouse`, which puts `CharacterMotor.Steer` on the branch that reads a
  heading as body-relative and never rotates the body. **Seat 0 travelled 224 m against 522 to 556
  for its siblings in Classic and 530 against 1133 to 1175 in Hero Strike; after the fix all four
  seats sit inside 5 per cent of each other on every arm, and the whole match got livelier with
  it**: Hero Strike throws 127 to 173, tags 77 to 102, and the unretrieved-slipper clock, which is
  a duration rather than an event count, **113 to 0**. Per-seat columns in an older report are not
  comparable, and neither is anything § 16 or § 17 measured.
- `AiDiagnosticProbe` runs one round at 1x with every decision written out, for WHY rather
  than how much. ⚠️ **`[Category("WallClock")]`, excluded from the default PlayMode run.** See
  § 7's command and `docs/TODO.md` § 6.
- `AspectRatioProbes` drives real layout through nine resolutions.
- `SceneScriptCheck` refuses a build scene holding a component the PLAYER cannot bind to a
  script. ⚠️⚠️ **It is the only check that can see this class of bug, because every other one
  runs in the editor and the editor resolves the broken reference by class name.** A shipped
  build crashed on the Ilalim ng Tulay map select with all of Core, EditMode, PlayMode,
  Headless, Arena, Audio and MapGeometry green. It reads scenes as TEXT on purpose: opening the
  scene is what hides the fault. `GameBuilder` runs it before every build.
- `MapGeometryCheck` refuses an arena whose props float, whose props are buried, whose floor
  has holes, or whose furniture stands inside the defender's box. ⚠️ **It found six faults on a
  map whose four showcase renders had already been signed off**, including both pavements
  floating 0.15 m over open air. A render only shows the angles somebody chose.
- `Checks.RunAll` runs `HeadlessCheck`, `ArenaCheck`, `MapGeometryCheck`, `AudioCueCheck` and
  `SceneScriptCheck` in ONE editor launch, and runs all five even after one fails. ⚠️ **The
  launches are the cost of a verification pass, not the assertions.** A full pass is three Unity
  launches plus `dotnet test`; it used to be seven. `GameBuilder` still runs `SceneScriptCheck`
  itself, deliberately: a build-time gate must not depend on somebody having run this first.
- `AbilityShowcaseProbe` photographs the ability TRANSIENTS as well as the persistent zones, and
  **fails a run where one blows more than 12 per cent of the frame to white**. That bound is
  `docs/VISION.md` § 2 rule 5 as a number, and the first run of it found Zack's ultimate at
  **62.8 per cent** against 8.3 for the worst of everything else.
- **Three `tools/` audits read the source as TEXT and answer questions no test can.** They need
  `python` (not on PATH; `%LOCALAPPDATA%\Programs\Python\Python312\python.exe`) and they exit
  non-zero, so they gate a verification pass:
  - `audit_ability_authority.py` walks every ability call that moves a body or writes score and
    reports whether a `NetAuthority.ShouldResolve()` gate is open at that brace depth. ⚠️ **Every
    `other` row must read HOST-ONLY.** It is currently 44 sites, 29 gated, **0 ungated on another
    body**; `docs/TODO.md` § 25.1 is the entry it was written for.
  - `audit_request_call_sites.py` reports every wire entry point in `Runtime/Net/` that nothing
    calls. ⚠️ **Tests deliberately do not count**: a test calling a request proves the method
    works, not that the game reaches it. It found three dead protocols and one verb that had never
    travelled at all (§ 38.5, § 38.3).
  - `audit_wire_payloads.py` compares each named message's writer and reader field by field.
    Netcode does not check that the two halves agree, and a field added to one is not an error, it
    is silently misread bytes (§ 38.6).
- `tools/` also holds player-side capture scripts.

Three faults from one session that no amount of playing would have found: a HUD string rebuilt
every frame cost the 6x probe an eighth of its frames and most of its physics steps; a slipper
came to rest 0.7 m outside the arena wall because the bounce only ran while in flight; and the
probe itself was unseeded, so the same build measured 110 and then 467 penalties on
consecutive runs.
