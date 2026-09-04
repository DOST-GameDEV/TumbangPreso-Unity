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
- ⚠️⚠️ **TICK IT OFF BY MOVING IT TO [`docs/TODO_Archive.md`](docs/TODO_Archive.md), IN THE SAME
  COMMIT, KEEPING ITS NUMBER.** `docs/TODO.md` is the OPEN worklist and nothing else. **It reached
  22,930 lines and 134 sections on 2026-09-03 and had stopped doing the one job it has**, which is
  to be read: 🧑, *"todo md so long can u clean that shit up"*, *"its not supposed to be that
  long"*. It is about 3,000 lines now and the archive holds the rest, whole.
  - **A section lives in `TODO.md` while its HEADING says `OPEN`, `IN PROGRESS` or `NOT DONE`.**
    Status goes in the heading, never buried in the prose. Prose status is exactly what made 134
    sections impossible to sort, and it is why the split had to be done by hand.
  - ⚠️⚠️ **A SESSION REPORT IS NOT AN OPEN ITEM. WRITE IT, THEN ARCHIVE IT IN THE SAME COMMIT.**
    *"The 2026-08-29 evening batch"* was 525 lines and *"the 2026-08-29 balance-and-controls
    batch"* was 973, and neither was ever open work. **Twelve of the twenty biggest sections in
    that file were dated batch reports** that nobody archived because nobody had told them to.
  - ⚠️ **NEVER DELETE ONE AND NEVER SUMMARISE IT AWAY.** The reasoning is the part that stays
    valuable and every ⚠️ in this repository was written because something went wrong once. The
    archive keeps whole bodies and unchanged numbers, and `TODO.md` keeps a one-line index row for
    each, **so every `docs/TODO.md` § N pointer in this file, in `VISION.md`, in `FUTURE.md` and in
    the code comments still lands on something that says where to look.**
  - ⚠️ **The section numbers are not unique** (§ 53, § 63, § 64 and § 65 each appear more than
    once) and that is not being fixed: renumbering would break every pointer in the repository.
    **Search by title as well as by number.**
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
  ⚠️⚠️ **AND SINCE 2026-09-02 THE RULE IS CHECKED PER DEVICE AS WELL AS PER CONTEXT, WHICH ADDED
  NO NEW CONCEPT.** A control PATH already carries its device, so `<Keyboard>/f` and
  `<Gamepad>/buttonNorth` are different controls and no press produces both. **What did change is
  that `FindDuplicateBindings` used to read only each action's FIRST binding**, which was the
  keyboard one: adding a pad binding beside every key would have doubled the map while halving
  what the rule checked, silently. `Rebinding.ResolveBindingIndices` returns all of them now, and
  `TryRebind` writes an override onto the binding for **the device the player just pressed** —
  targeting index 0 wrote the pad's path over the KEY, and Reset All was the only way back.

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

## 4a · ⚠️⚠️ THREE DEVICES, EVERY TIME. MOUSE AND KEYBOARD, CONTROLLER, AND TOUCH

🧑 asked for this twice, and the second time was stronger than the first: *"make that shit future
proof and to update mobile and controller version every time we change ui or some shit"*, then
**"anytime we add a feature, make sure all controller and mobile is considered"**. The second
sentence is the rule: **not just UI. Any feature.**

⚠️⚠️ **AND THE REASON IT IS BUILT INTO THE CODE RATHER THAN WRITTEN AS A CHECKLIST IS THAT A
CHECKLIST IS EXACTLY WHAT FAILED HERE THREE TIMES.** Every one of these was a rule somebody was
supposed to remember, and a move that nobody updated:

| | What went stale | What it cost |
|---|---|---|
| `docs/TODO.md` **§ 96** | The hub had one door, a corner chip, and **the person who commissioned the hub never found it.** `PlayerHubLayoutProbe` was green at all nine resolutions the whole time. | The probe asserted the plate was on screen. That is not the same claim as "somebody can reach it". |
| **§ 114** | `PlayerNameplate` was no longer installed by any screen, and `PlayerHubLayoutProbe` still drove it. | A probe measuring a control the game no longer builds. |
| **§ 124.11** | `LoadoutSurfaceProbe` was knocking on a door § 122 had moved, and had been failing before that session started. | *"A green probe for a screen nobody can reach is worse than a red one"*, and a red one for a screen that works teaches the next reader to skim the results. |

**So the answer is construction, not discipline.** Four things now make forgetting impossible, and
each replaced a place where remembering was the only protection:

- ⚠️⚠️ **A NEW `Verb` DOES NOT COMPILE UNTIL IT HAS A PAD BINDING AND A THUMB TARGET.**
  `InputLayer.InputCatalogue.For` is a switch expression with **no discard arm**, and
  `Assets/TumbangPreso/Runtime/csc.rsp` turns the resulting CS8509 into an error. Every field of
  `VerbInput` is a constructor parameter with no default, so there is no way to half-answer.
  This is `HeroAbility.Glyph` and `TelegraphRadius`'s argument applied to input: *a lookup table
  keyed by id is a second place to forget, and forgetting it compiles.* **Do not add a `_ =>` arm
  to that switch, and do not delete that `.rsp`.**
- ⚠️⚠️ **A NEW SCREEN GETS A FOCUS PATH AND THUMB-SIZED HIT AREAS BY CONSTRUCTION.**
  `MenuKit.BuildCanvas` and `ConvertedScreen.Start` both install `InputLayer.ScreenFocus`, and
  those two are every screen in the game. `ConvertedScreen` already made this argument for the
  mouse cursor in its own words: *"doing it in the base class means a screen added later cannot
  forget."*
- ⚠️⚠️ **AND `InputSurfaceCheck` REFUSES A BUILD WHOSE SOURCE BUILDS A CANVAS OUTSIDE THE KIT.**
  It reads the runtime sources as TEXT, for `SceneScriptCheck`'s reason one level up: every other
  check can only see a screen that was OPENED, so a screen nobody opens during a test run has no
  coverage at all, which is § 96 and § 124.11 in one sentence. It is in `Checks.RunAll`.
- ⚠️ **`InputSurfaceProbe` DISCOVERS SCREENS INSTEAD OF LISTING THEM**, from the build settings
  and from the assembly, at the nine desktop shapes **and two phone shapes and his own short wide
  window** (`ProbeResolutions`). ⚠️⚠️ **`UiClickProbe` still carries a hard-coded list of five
  screens and is the § 124.11 fault pre-installed.** Leave it, but never copy it.

**What this asks of you, concretely, for anything you add:**

1. **A new verb**: the compiler stops you. Answer the pad and the thumb, then run
   `InputAssetSync.Regenerate` so the `.inputactions` asset catches up. `InputContractTests` fails
   until you do.
2. **A new non-verb action** (a round action, a spectator key): add a row to
   `ScreenInputCatalogue`. ⚠️ **A `null` pad path is a legal answer and a written-down one**;
   silence is not. `ToggleFullscreen` is the example: a phone has no window.
3. **A new screen**: build it through `MenuKit` or `ConvertedScreen` and you are done. If you
   think you need a bare `Canvas`, you are about to ship a screen a pad and a thumb cannot use.
4. **A new feature that is not a screen or a verb**: ask the three questions out loud before you
   call it done. *How is this reached on a pad? What does a thumb press? What does the prompt say
   on each?* ⚠️ **Prompts read the live binding through `Rebinding.DisplayNameFor(asset, action,
   device)`, never a literal** — `docs/VISION.md` § 3: *a screen that teaches the wrong key is
   worse than one that teaches none.*

⚠️⚠️ **CROSSPLAY IS A SEPARATE CLAIM AND INPUT MUST NEVER TOUCH IT.** `NetSession.ProtocolVersion`
is the match FORMAT, and peers on different numbers refuse each other **by design**. A pad, a
thumb and a keyboard all arrive at `InputIntent` and **nothing about which device was used goes on
the wire**, so an input change may never move that constant.
`InputContractTests.TheInputPassDidNotMoveTheProtocolVersion` asserts the number, so a legitimate
bump is a deliberate act. ⚠️ **When it does move, the Windows and Android players must be rebuilt
from the same commit and shipped together**, or they refuse each other correctly and it reads as a
bug (`docs/FUTURE.md` § 15). The UGS project is `dcf0831e-a5f4-43b4-832e-b687f13a3569`, org
`matthewtlabrador`: **a machine on a different project resolves a join code in a different
namespace and reads as an empty lobby rather than as an error.**

⚠️ **THE MATCHMAKING POOLS STAY SEPARATE AND THAT IS NOT A CONTRADICTION.** `FUTURE.md` § 14:
*"No aim assist. Separate the pools instead, which is free, exact, and removes the argument."*
`Matchmaker` already carries `InputDevice` in the pool key. **Crossplay is for lobbies, join codes
and LAN; the ranked queue still bands by device.** Both are true at once.

⚠️⚠️ **AND SINCE 2026-09-03 THE CODE ACTUALLY DOES THAT, WHICH IT DID NOT BEFORE.**
`MatchmakingRules.PoolKey` banded **both** stakes by device and platform, so the sentence above was
true of this file and false of the game: a phone and a PC could not meet through QUICK MATCH at
all, only by typing a join code at each other. 🧑 2026-09-03: *"i want a mobile and a pc to be able
to play tgthr"*. **Casual is one crossplay pool (`v21.Classic.Casual`, three parts) and ranked
keeps all five.** The argument the banding protects is a LADDER argument, and nobody disputes a
casual match. `docs/TODO.md` § 130.4.

⚠️⚠️ **AND THE OTHER HALF OF CROSSPLAY WAS NEVER THE NETWORK LAYER.** `ApproveConnection` reads the
protocol, capacity and the block list, and nothing about a device goes on the wire, so the
architecture was always right. **What was broken was the phone**, twice, and both are in § 130:
`NetIdentity` cached a FAILED sign-in for the life of the process (§ 130.2, and on Android the boot
attempt fires while the handset is still associating with wifi, so one bad moment made every later
JOIN BY CODE fail with no cure but force-closing the game), and `Shader.WarmupAllShaders()` ANR'd
the app before it ever reached the menu (§ 130.5). **Check those two before believing a crossplay
report.**

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

⚠️⚠️ **SOURCED SFX ARE PROVISIONAL UNTIL 🧑 HEARS THEM IN PLAY.** He rejected the replacement
can hit, can down and button hover by name, and their original WAVs are restored. If he asks to
restore another sound, read [`docs/Asset_Sourcing.md`](docs/Asset_Sourcing.md) § 5.5 and
[`Attention.md`](Attention.md) § 13 first. The old blobs are at `ee8bced^`; restore only the named
target, resolve aliases, and move it from `tools/build_ability_audio.py.REPLACEMENTS` to `KEPT` in
the same commit so the generator cannot put the rejected sound back. **Never roll back the whole
asset pass to restore one cue.**

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

### 6.2 ⚠️⚠️ THE STANDING BRIEF FOR EVERY SCREEN: INTUITIVE, EASY TO GET AROUND, NEVER OVERWHELMING

🧑, twice, unprompted: *"i want the user experience of movinng around the game to feel
intuitive"*, and *"i wwant the user experience for the UI of this app to feel intuitive and easy to
navigate and not overwhelming"*.

**This is the acceptance test for every screen in the game, and it is three separate claims. All
three have already failed here, each with its own receipt.**

| The claim | What failing it looks like | The receipt |
|---|---|---|
| ⚠️⚠️ **INTUITIVE** | The player cannot predict what a control does before pressing it, or presses something and nothing happens. | `docs/TODO.md` § 108: an EQUIP button with no `onClick` listener, and a CUSTOMIZE LOADOUT button opening a screen drawn underneath the screen that opened it. Both looked fine. Both did nothing. |
| ⚠️⚠️ **EASY TO NAVIGATE** | The player cannot FIND the thing, or cannot get back out. | § 96: the hub had exactly one door, a corner chip reading a name and a level, and **the person who commissioned the hub never found it.** § 6.3 is the method. |
| ⚠️⚠️ **NEVER OVERWHELMING** | Everything the feature can do is on screen at once, in one flat list, with nothing saying what matters. | § 92: *"theres liek 20 shits at once"*, six buttons in six visual languages. § 94.7: *"its so messy and ugly"*, seven readability faults with every probe green. |

**So, before writing a screen and again before calling it done, answer these in one sentence each:**

1. **What is the ONE thing on this screen?** Everything else is sized, placed and coloured against
   it. If two things are competing, one of them is decoration.
2. **What is the first press, and can the player guess it?** Name it out loud. A control that has
   to be discovered rather than read is the bug.
3. **What is on screen that the player does not need RIGHT NOW?** Collapse it, move it behind a
   section header, or cut it. ⚠️ **A group closed by default with a one-line summary on its
   header beats the same rows always open**, and the summary is what makes it worth opening.
4. **How do they get out, and is it one press?** Escape, always, innermost layer first.

⚠️ **AND THE TEST FOR ADDING ANYTHING IS WHAT THE PLAYER HAS TO HOLD IN THEIR HEAD, NOT WHAT IT
COSTS TO BUILD.** 🧑, asked which features to drop: *"i have ai dont think abt 5 students shit"*,
and *"the cutting shit i want should be focused onn things that overcomplicate game for ppl"*.
A cheap addition that adds a bar, a screen, a number or a new word is still a candidate for
cutting. `docs/FUTURE.md` § 0.5 rule 11b.

⚠️ **THE THREE SECTIONS BELOW ARE HOW THIS BRIEF IS MET, NOT SEPARATE RULES.** § 6.2b is what to
take a picture OF, § 6.2c is the four questions about every rectangle, and § 6.3 is the journey
between screens. **None of the three is visible to any probe in this repository**, which is why
they are here rather than in a test.

### 6.2a Every screen gets designed, and the method is written down

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
| ⚠️⚠️ **Is this width measured against the NARROWEST box it will ever live in?** | **A control is sized against 4:3, never against 1920.** `UiRows.Cap` records the number: the value column is about **368 units at 4:3** and every control in that file fits inside it. A width chosen at the reference resolution is a width that only exists at the reference resolution, and the failure is silent because `MenuKit.Label` OVERFLOWS rather than wrapping: the control does not shrink, it draws over its neighbour or off the edge. | `docs/TODO.md` § 108: the first `StepperRow` laid out to 476 units, so at 1366x768 **the right-hand arrow was simply not on screen**, and the row's own hint drew straight through the value beside it. The layout probe was green: every label fitted its own box, and the boxes overlapped each other. |
| ⚠️⚠️ **If I delete this, what else was it doing?** | **Anything covering the screen is also eating clicks, and the block is usually nobody's stated job.** When a full-screen graphic goes, name its replacement blocker in the same commit. | § 100: the scrim was silently what stopped a press on the art side reaching the title screen underneath. Deleting it would have let a player press PLAY **through** the boot screen, on the one screen that exists to ask a question first. The key art is the blocker now, and it says so. |

⚠️⚠️ **AND NONE OF THE FOUR IS VISIBLE TO ANY PROBE IN THIS REPOSITORY, WHICH IS THE POINT OF
PUTTING THEM HERE.** `PlayerHubLayoutProbe` was green through every one of them, because a label
that fits its box fits its box whether the picture behind it is beautiful or butchered. **The probe
asks whether the screen is a screen. This section is what to look at in the picture.**

### 6.3 ⚠️⚠️ MOVING AROUND THE GAME IS ITS OWN DESIGN PROBLEM, AND THE UNIT IS THE JOURNEY

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

### 6.4 ⚠️⚠️ NEVER USE BLUE OR NAVY ANYWHERE IN THE UI. NOT OUTLINES, NOT FILLS, NOT BACKGROUNDS, NOT GREYS WITH A BLUE CAST

🧑 2026-08-31: *"i dont like blue outlines its out of theme"*, *"can u put in claude md to
never use blue outlines and shit for ui"*. Then 2026-09-01, having opened a build with § 6.4
already in this file: *"i dont want to see blue shit"*, *"thats not in theme"*, *"hey i said i
dont want blue or navy"*, *"thats off theme"*, *"put in claude md to not use blue hshit"*.

⚠️⚠️ **HE HAD TO SAY IT FIVE MORE TIMES BECAUSE THE FIRST VERSION OF THIS SECTION SAID
"OUTLINES", AND THE BLUE HE WAS LOOKING AT WAS NOT AN OUTLINE.** It was four things, and every
one of them passed a reading of the narrow rule:

| Where | What it was | Why it was invisible to the narrow rule |
|---|---|---|
| ⚠️⚠️ **`UiTheme.Ink`** | `040838`, a near-black **navy**, and the outline colour of **every** menu type style in `GodotTheme` (`MenuDisplay`, `MenuHeading`, `MenuBody`, `MenuCaption`, `MenuValue`). | It was called "ink", so it read as black at a glance and as a cold ring at four to six pixels on a heading. **One constant put navy on every word in the front end.** It is `1c0f06` now. |
| **The character select backdrop** | Three stops of slate-to-midnight, with a comment calling it *"the game's Bayan navy identity"*. | It is a background, not an icon. Nothing else in the front end uses that colour, so the "identity" was one screen's. Wood now. |
| **`MatchResult` and `RoleSwapCard` scrims** | The same navy at 72 and 82 per cent. | Same. |
| **`UiTheme.Panel` and `Card`** | `e1e5e8` and `f5f7fa`: greys with a blue cast, used as the fill under form fields. | "Grey" is not "blue" until it is sitting next to `8b5227` wood, where it reads cold. Warm paper now. |
| ⚠️⚠️ **`GameBuilder.ConfigureSplash`** | Wrote the navy splash background and the studio logo **on every build**. | **A colour set in `ProjectSettings.asset` is not set.** This method overwrites `backgroundColor`, `logos` and `unityLogoStyle` every time, so an inspector change survives until the next build and then reverts with no error. **Both places or neither.** |

- **The rule, stated wide:** no blue, no navy, no cold grey, in any UI colour, in any layer.
  Outlines, fills, panel backgrounds, scrims, rings, gradients, glyph tints, disabled states.
  **If a hex has more blue in it than red, it does not belong in a menu.**
- ⚠️⚠️ **THE PALETTE, AND IT MOVED ON 2026-09-03. IT IS THE LOGO'S NOW.** 🧑: *"the colors are
  final, ask it to use the same colors as logo"*. `docs/TODO.md` § 133.1 is the entry and
  `docs/Front_End_Design.md` § 4 is the role table.

  | Name | Hex | Share of the logo | Its ONE role |
  |---|---|---|---|
  | **Deep red** | `#980715` | 34.3% | The **outline**, everywhere, plus the one destructive control. Never a ground. |
  | **Honey Quartz** | `#FCD39F` | 23.1% | The **ground** of every light screen, and the base the paper ramp is tinted from. |
  | **Chartreuse** | `#D6CE01` | 17.0% | The **action**. One per screen, the primary only. |
  | **Persimmon** | `#FD8041` | 5.7% | The **marker**: the one value or selection that matters. |
  | **Golden** | `#F5B521` | 4.2% | The front end's gold. ⚠️ **`UiTheme.Amber` is still `#FFBA00`** because the HUD reads it 15 times and the HUD is out of scope. |
  | **Rim red** | `#C32E0D` | 3.8% | The lit state of the deep red, drawn as exactly that in the mark. |
  | **Army** | `#B3A828` | 1.4% | The **dark ground**, and the only one: the fighter picker's stage. |
  | **Khaki** | `#E8C77E` | derived | The quiet mid-tone. ⚠️ **The one colour here that is derived rather than measured**, because the drawing never needed one; `Attention.md` § 12 carries the ask to confirm it against his swatch strip. |

  ⚠️⚠️ **EVERY ONE OF THOSE WAS MEASURED BY A SCRIPT, NOT PICKED.** `tools/read_brand_palette.py`
  clusters the committed artwork's flat fills; the percentages are its output, and it **agreed
  with itself across two independently drawn files** (`tump_logo_colour.jpg` and
  `tsinelas_hit.jpg`). ⚠️ **Re-run it rather than eyeballing a new logo**: the masters arrive as
  JPEG, and its first pass reported the outline as EIGHT different colours before it learned to
  merge chroma-subsampled values.

  ⚠️ **The paper ramp is ONE colour at four tints** (`Paper` `#FEEBD4`, `PaperWarm` `#FDDFBA`,
  `PaperEdge` `#FCD39F`, `PaperSunk` `#DEBA8C`), all derived from Honey Quartz, which is § 6.5's
  *"one base colour generates a whole control"* moved up a level. The ink is a MIX of the two
  darkest brand colours rather than pure red, because red text means "something is wrong" in
  every convention a player owns; `#55290F` measures **10.5:1** on the page and `#97491B`
  measures **5.5:1**, both computed by `scratchpad/fontsrc/ramp.py` rather than judged.

- ⚠️ **THE CARVED WOOD IS THE OLD PALETTE AND IS KEPT RATHER THAN DELETED**: `#31190B` deep,
  `#5A2F14` mid, `#8B5227` edge, `#1D0E06` dark, cream `#F5E6C8`, amber `#FFBA00`, warm ink
  `#1C0F06`. Two reasons, neither sentiment: **`PaperPurityProbe.WoodFills` lists those exact
  hexes to DETECT a leftover** from the old front end, so deleting them blinds the gate that
  proves the overhaul finished, and **the in-match HUD is still drawn in them on purpose**
  (§ 133.4). Geometry still comes from warm tone-on-tone bevels and borderless shapes.
- ⚠️⚠️ **THE ONE EXEMPTION, AND IT IS A GAMEPLAY FACT RATHER THAN A STYLE: `UiTheme.Defense`
  (`0080e8`) MEANS "THE TAYA".** It is the defending side's colour in the match, opposite
  `Offense` orange, and it is the only blue in the project that may be drawn. **It may never
  appear as menu chrome**, which is what `ChatAndLobbyChromeTests` asserts for the lobby
  nameplates: a decorative blue that happens to be the role colour teaches the player a role that
  is not there.
- ⚠️ **THE AUTHORED PENNANT ART IS NOT THIS EITHER.** PLAY green, SETTINGS yellow, TUTORIAL blue
  and QUIT red are 🧑's own nine-patch art and `docs/VISION.md` § 6 says his UI art IS the design
  system. **Do not repaint his art to satisfy this rule.** This section is about colours chosen
  in code.
- ⚠️ **CHECK IT BY GREPPING, NOT BY LOOKING.** `UiTheme.Ink` was navy for the entire life of this
  file and nobody saw it, because a near-black navy looks black in a code review and blue on a
  1440p screen at six pixels of outline. `grep -rnE 'Hex\("[0-9a-f]{6}"\)' Assets/TumbangPreso/Runtime/UI/UiTheme.cs`
  and read the third channel.

⚠️⚠️ **THIS SECTION WAS INSERTED BETWEEN § 6.3'S HEADING AND § 6.3'S BODY, so for one commit
§ 6.3 was an empty heading and every word of the journey rule read as if it belonged to a rule
about outline colour.** It is here, after § 6.3 finishes, for that reason. A heading with no body
is not a formatting slip in this file: § 6.3 is the section `docs/TODO.md` § 96 and § 92 both point
at, and a reader who followed either pointer landed on nothing.

- ⚠️ **THE ORIGINAL, NARROW VERSION OF THIS RULE READ: "UI icons, rank emblems, state badges,
  glyphs and panels must never carry dark blue, navy or cold ink OUTLINES."** It is kept here
  because it names the surface the fault was first found on (the rank emblems), and because the
  gap between it and the table above is the whole lesson: **a rule written against the one place
  a fault was seen does not cover the constant that caused it.**
- ⚠️ **THIS IS `docs/VISION.md` § 6 APPLIED, NOT A NEW RULE.** *"His UI art is the design system.
  Wood, amber, cream, ink. Anything drawn in a different visual language is the thing that looks
  broken, not the thing that looks new."*

### 6.5 ⚠️⚠️ THE FRONT END IS DRAWN IN HIS ART'S OWN GEOMETRY, AND `WoodCraft` IS WHERE THAT LIVES

🧑 2026-09-01, after a whole pass that had already replaced every button and every plate:
*"ui still looks unnatural and ugly"*, then the cause in his own words: *"the issue with old UI is
everything feels repetitive bcz i think u use the same code to generate them all"*, and
**"make sure all ui isnt generated in the same way but follows a central theme bcz old issue was
it read as repetitive with everyone just being brown and boring"**.

⚠️⚠️ **§ 6.4 FIXES THE PALETTE AND THAT WAS NEVER THE PROBLEM. THE PROBLEM WAS THE SHAPE.**
Sampling `Assets/TumbangPreso/Art/ui/host-game/*.png` pixel by pixel: **every surface he authored
is a chamfered or rounded slab with a BRIGHT keyline outside a DARK rim, over a full-height
gradient with a varnish band a quarter of the way down.** Every surface drawn in code was a
rounded rectangle with a DARK outline over a FLAT face. **The lobby draws both at once**, because
`StartButton` is his own `BUTTON LONG` texture, so his art and the code sat in one 460-unit rail
in two opposite visual languages and the code-drawn half was the one that looked wrong.

- **`Runtime/UI/WoodCraft.cs` is the transcription and its header carries the measurements.** Read
  it before drawing any new surface. `JOIN BUTTON.png` is `BUTTON LONG.png` with one colour
  swapped, keyline to floor, so **one base colour generates a whole control** and the ratios are
  stored as multipliers on HSV value rather than as hexes.
- ⚠️⚠️ **THE PAPER FRONT END HAS ITS OWN PRIMARY NOW, AND IT IS THE ONE CHAMFER ON A CREAM SCREEN.**
  `PaperCraft.Surface.Action`, added 2026-09-02. Until then the one action on every paper screen was
  still `GodotTheme.WoodPrimaryButton`, so START MATCH, CREATE ACCOUNT, KEEP AND USE and CHOOSE were
  wooden objects standing in rows of paper ones. 🧑 found it on four screens without connecting them
  (*"i dont get why theres rounded sshit next to square shit"*, *"it feells so flat"*, **"can u js
  remake the entire start match button"**), and the measurement is in `docs/TODO.md` § 121.1: the
  wooden halo sampled `ada69b`, **hue 37 at 10 per cent saturation**, against every paper edge on the
  same screen at **30**. A neutral that dark and that grey beside warm cream is § 6.4's cold-grey ban
  caught on the warm axis. ⚠️ **The `Accent` beside it is a closed list of two colours he authored**
  (his green, and the lobby's brown he asked to keep), not the `fill` parameter this section forbids:
  one role, one per screen, two authored fills.
- ⚠️⚠️ **AND `PaperKit.MakeAction` SWITCHES OFF EVERY CHILD GRAPHIC, WHICH IS THE ONLY REASON IT
  WORKS.** `PaperKit.Paperise` disables `GodotButton` and the two `SkinLayers` children it knows by
  name; `ArrowButtonView` builds three more (`Artwork`, `Lit`, `Rim`) that nothing had heard of, so
  the first build drew **his chamfered `BUTTON LONG.png` on top of a new surface** and 🧑 photographed
  it as *"its a circle and a sharp shape at the same time"*. **Disabling a component does not remove
  the objects it made.** This is the one place in the front end that stops drawing an authored
  control, he asked for it by name, and the file, the main menu and the unfurl are untouched.
- ⚠️ **PICK A ROLE, NOT A FILL.** `WoodCraft.Surface` is a closed list (`Button`, `Action`,
  `Panel`, `Header`, `Field`, `Paper`, `PaperField`, `Slate`) and the material, silhouette, relief
  and colour all follow from it. The failure this replaced is a screen of twelve plates that were
  all one call with a different fill, and the way that happened is that the fill was a parameter.
- ⚠️ **A CHAMFER MEANS PRESSABLE AND A ROUND MEANS FURNITURE**, in his art with no exception. A
  shape difference survives a photograph and a colourblind player; a fill difference does not.
- ⚠️⚠️ **AND "BROWN AND BORING" NEEDED A SECOND ANSWER: cream and asphalt are SURFACES, not just
  text colours.** His login fields are cream plates and `VISION.md` § 2 rule 5 names the chalk and
  the road. Paper and Slate are built by different rules from wood (no keyline, no ramp, no
  bevel), so they cannot read as another plank.
- ⚠️ **`WoodSkin` OR THE SPRITE IS WRONG.** A slab is sliced horizontally only, so it is correct
  at exactly the height it was built for; every rect in this front end is driven by a layout group
  or an aspect-scaled canvas, so no caller can know its own height when it builds itself. The
  component watches the rect. `GodotButton` and `GodotPanel` carry the same watch for the
  converted screens, which is how one edit reached every button and every panel in phases 1 to 12.
- ⚠️ **NOTHING IN IT REPAINTS HIS ART.** The pennants, `BUTTON LONG`, `JOIN BUTTON`, the arrows
  and the key art are still drawn from the PNGs. This is the surface AROUND them.
- ⚠️⚠️ **GREEN IS HIS PRIMARY COLOUR AND IT IS EVIDENCE, NOT TASTE.** `JOIN BUTTON.png` and the
  `PLAY` pennant are both authored green. `UiTheme.MenuGreenFace` is the measured peak; `MenuGreen`
  `21a131` is a third darker than any pixel in his button and produced a bottle-green slab.

⚠️⚠️ **AND EVERY ONE OF THE SIX FIXES IN `docs/TODO.md` § 117.7 WAS INVISIBLE IN THE SOURCE AND
OBVIOUS IN A RENDER.** A live tab that three comments in this repository call *"four units
taller"* was never taller in any build, because `childForceExpandHeight` silently overrides every
`LayoutElement` under it. A chalk rule at 0.55 alpha is a quarter-strength mark, because the tint
multiplies the sprite's own. **Take the picture, then take it again.**

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
"/c/Program Files/Unity/Hub/Editor/6000.5.8f1/Editor/Unity.exe" -batchmode -runTests -projectPath . -testPlatform PlayMode -testCategory "!WallClock;!ThumbFloor" -testResults Logs/play.xml -logFile Logs/play.log
```

⚠️⚠️ **`!ThumbFloor` IS THE SECOND EXCLUSION AND IT IS A SHRINKING GAP RATHER THAN A FLAKE.**
`InputSurfaceProbe.TheFrontEndMeetsTheThumbFloor` measures every menu control against the
144-unit touch target floor. It read **1519 measurements under it across 12 shapes** until
2026-09-03 and reads **50** now, all near misses (`docs/TODO.md` § 126.2 and § 126.12).

⚠️⚠️ **THE CAUSE WAS NOT "NOT ENOUGH PADDING", IT WAS "NOWHERE TO PAD INTO", AND EVERY ONE OF THE
1519 SAID SO.** Each reported a size exactly equal to the control's own artwork, which means the
pad had grown by ZERO units: `ScreenFocus.ApplyTouchTargets` takes half the gap to a neighbour and
these rows are stacked with no gap at all. `ScreenFocus.MakeRoomForThumbs` now raises the layout
row a group actually owns, forces a rebuild, and pads after, so the growth has somewhere to go.
⚠️ **The half that matters is that `EveryScreenHasAFocusPathAndReachableTouchTargets` still
passes**: that is the check that a press at a control's centre lands on that control, and it is
what caught the padding bug in § 125.4. Making forty rows taller stole no presses.

⚠️⚠️ **RUN IT ALONE.** `InputSurfaceProbe` loads every scene in the build settings and opens every
overlay it can discover, so it is the most destructive fixture in the suite: in a twelve-suite run
it took most of the group down with it and the numbers were meaningless. See § 126.8.

Run it on purpose with `-testCategory "ThumbFloor"`; the failure message is the worklist, and
`Logs/input-surface.txt` carries the whole sweep including the scrollbars it exempts and any note
that the camera was replaced part way through, which means fewer shapes were measured.

⚠️⚠️ **`-testCategory "!WallClock"` IS PART OF THE COMMAND, NOT AN OPTIMISATION.**
`AiDiagnosticProbe` runs a round at 1x for about 80 real seconds by design, so its result depends
on how busy the machine is: it has failed at 21.6 s, 29.9 s and 37.6 s against a 20.0 s bound and
passed on immediate re-runs with nothing changed. `docs/TODO.md` § 6 carries the decision.
⚠️ `[Explicit]` does NOT do this in batch mode; it was tried and the tests still ran.
Run them on purpose with `-testCategory "WallClock"`.

**All SEVEN editor checks, in one launch.** ⚠️ This line said *"all five"* until 2026-09-03, and
§ 7.1 four hundred lines down has listed seven since `InputSurfaceCheck` and
`ShaderWarmupCollection` joined: `HeadlessCheck`, `ArenaCheck`, `MapGeometryCheck`,
`AudioCueCheck`, `SceneScriptCheck`, `InputSurfaceCheck`, `ShaderWarmupCollection`. **A count in
one place and a list in another is § 5's drift rule inside this file**, and the count is the copy
that goes stale.

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

⚠️⚠️ **AND THERE IS A THIRD STATE, FOUND 2026-09-03, WHICH IS WORSE THAN THE CRASH: A `.xml` THAT
SAYS `result="Passed"` AND `total="0"`.** A crash writes no file at all and is at least visibly
absent. This one is present, well formed, and green:

```xml
<test-run id="2" testcasecount="0" result="Passed" total="0" failed="0" duration="0.4359008">
```

**So read `total` and `failed`, not `result`.** `docs/TODO.md` § 126.8c has both causes, and they
produce byte-identical files from thirteen minutes apart:

- **Something destroyed the runner's own objects mid-run.** Every test still executes; there is
  simply nothing left to write the results down. `PlayModeWorld.NeverTouch` is the guard.
- ⚠️⚠️ **`-testFilter` IS SEMICOLON-SEPARATED, NOT COMMA-SEPARATED.** A comma-joined list of
  fixture names is read as one impossible name, matches nothing, and produces exactly the same
  file in thirteen seconds.

```bash
"/c/Program Files/Unity/Hub/Editor/6000.5.8f1/Editor/Unity.exe" -batchmode -runTests -projectPath . -testPlatform PlayMode -testCategory "!WallClock;!ThumbFloor" -testFilter "TumbangPreso.PlayTests.SteeringTests;TumbangPreso.PlayTests.CarryTests" -testResults Logs/targeted.xml -logFile Logs/targeted.log
```

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
- `Checks.RunAll` runs `HeadlessCheck`, `ArenaCheck`, `MapGeometryCheck`, `AudioCueCheck`,
  `SceneScriptCheck`, `InputSurfaceCheck` and `ShaderWarmupCollection` in ONE editor launch, and
  runs all of them even after one fails. ⚠️ **The last one REGENERATES rather than inspecting**:
  it rewrites the `ShaderVariantCollection` the loading screen warms a slice per frame out of, and
  a collection that is checked but not rewritten goes stale the first time somebody adds a material
  and then warms the wrong shaders while looking exactly like a working one. `GameBuilder` rebuilds
  it on every build for the same reason `ConfigureSplash` rewrites the splash: **both places or
  neither** (§ 6.4). `docs/TODO.md` § 130.5. ⚠️ **The
  launches are the cost of a verification pass, not the assertions.** A full pass is three Unity
  launches plus `dotnet test`; it used to be seven. `GameBuilder` still runs `SceneScriptCheck`
  itself, deliberately: a build-time gate must not depend on somebody having run this first.
- `AbilityShowcaseProbe` photographs the ability TRANSIENTS as well as the persistent zones, and
  **fails a run where one blows more than 12 per cent of the frame to white**. That bound is
  `docs/VISION.md` § 2 rule 5 as a number, and the first run of it found Zack's ultimate at
  **62.8 per cent** against 8.3 for the worst of everything else.
- **The `tools/` audits read the source as TEXT and answer questions no test can.** They need
  `python` and they exit non-zero, so they gate a verification pass.
  ⚠️⚠️ **AND WHETHER `python` IS ON PATH DEPENDS ON WHICH LAPTOP YOU ARE ON, WHICH IS WHY THIS NO
  LONGER ASSERTS EITHER.** This line read *"not on PATH"* flatly until 2026-09-04, and on the
  `Matthew` profile that is simply false: `Python312\` and its `Scripts\` are both in the USER Path
  and `python --version` answers 3.12.10 from PowerShell and from bash. **There are two machines
  with two profiles** (`CLAUDE.md` § 7 already records `GameBuilder.BuildWindows` resolving to
  `C:\Users\matth\Desktop` on one and `C:\Users\Matthew\Desktop` on the other), and a note that is
  true on one of them and stated as a fact about "here" sends whoever is on the other one hunting.
  **Try `python` first; fall back to `%LOCALAPPDATA%\Programs\Python\Python312\python.exe` if the
  bare command is not found.** Both work on this profile.
  ⚠️⚠️ **THIS LINE SAID "THREE" WHILE THE FOLDER HELD SIX, WHICH IS § 5'S DRIFT RULE CAUGHT IN THIS
  FILE FOR THE SECOND TIME** (the first was "all five editor checks" against a list of seven, three
  hundred lines down). **The list below is the authority and there is deliberately no number**: a
  count is the copy that goes stale, every time, because the person adding the seventh audit edits
  the list and not the sentence above it. `ls tools/audit_*.py`.
  ⚠️ **AND `PYTHONIOENCODING=utf-8` IS REQUIRED ON THIS MACHINE**, or `audit_audio_reach.py` dies
  on a `UnicodeEncodeError` part way through its own output, which looks like a crash in the thing
  it is auditing.
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
  - `audit_audio_reach.py` and `audit_presentation_reach.py` ask whether a cue or an effect
    reaches every peer or only the host. ⚠️ **The first one LIED for its whole life until
    2026-09-04**: it was the only audit that did not strip comments before looking for a gate, so
    `NetCue`'s own header explaining the gate it replaces registered as a gate and reported
    `NetCue.Play` itself as host-only. A reader trusting that goes hunting for a bug in the fix.
  - `audit_cue_relay.py` answers the other half: which cues are relayed AND played locally, and
    therefore double-fire. ⚠️⚠️ **READ ITS HEADER BEFORE TOUCHING IT.** The first version was wrong
    about 42 of 48 rows because **the gate is usually in the CALLER**, so gatedness is propagated
    to a fixed point. It carries two allowlists, `WRAPPED` and `OWNER_DRIVEN`, and both **assert
    the line that makes their claim true still exists**, so deleting one fails here rather than
    going quiet in a match.
- **`tools/net_link.py` and `tools/net_matrix.py` put two real players on a link this machine
  controls**, which is how the disconnect matrix and the bad-wifi table in `docs/TODO.md` § 137
  were measured. ⚠️⚠️ **DO NOT REACH FOR `UnityTransport.SetDebugSimulatorParameters` INSTEAD.**
  It and `DebugSimulator` are both `[Obsolete]` **with no effect** in netcode 2.13.1, and the
  simulator pipeline stage is only configured under `UNITY_MP_TOOLS_NETSIM_IMPLEMENTATION_ENABLED`,
  a define from `com.unity.multiplayer.tools`, which is not in the manifest. **A table built on it
  compiles, runs, and measures a perfect link.**
- `tools/` also holds player-side capture scripts.

Three faults from one session that no amount of playing would have found: a HUD string rebuilt
every frame cost the 6x probe an eighth of its frames and most of its physics steps; a slipper
came to rest 0.7 m outside the arena wall because the bounce only ran while in flight; and the
probe itself was unseeded, so the same build measured 110 and then 467 penalties on
consecutive runs.
