# CLAUDE.md

> **Condensed 2026-09-24 at the owner's request** (*"optimize and clean up Claude.md without removing
> any important instructions"*). Every rule is still here; the incident stories behind them are cut
> to one line each. The full previous text, receipts included, is
> [docs/archive/CLAUDE_full_2026-09-24.md](docs/archive/CLAUDE_full_2026-09-24.md). Section numbers
> are unchanged, because the code and docs point at them.

## 0 · Routing and standing owner notes (read first)

- **Read order: [AGENTS.md](AGENTS.md), [docs/VISION.md](docs/VISION.md), [docs/TODO.md](docs/TODO.md),
  then the newest pointer in [the ledger](docs/ACTIVE_REWORK_LEDGER.md).** AGENTS.md is the primary
  instruction file (owner, 2026-09-12); this file keeps the rules and their reasons. Older dated
  scope statements here never override AGENTS.md or newer owner instructions.
  [docs/README.md](docs/README.md) indexes everything else.
- **Docs layout (2026-09-23):** `docs/TODO.md` is the queue plus one index row per numbered entry;
  open entry bodies live in `docs/TODO_Backlog.md`; superseded plans are in `docs/archive/`.
  Current priority: VISUAL-1 (visual communication and appeal) after the in-flight 134.10 link.
- **Lanes:** C1 to C3 have a documented handback; C4 stays reserved. TODO149.4 request safety is
  reserved under [docs/CLAUDE_REQUEST_SAFETY_LANE.md](docs/CLAUDE_REQUEST_SAFETY_LANE.md). A separate
  Claude PC works only [docs/CLAUDE_ENGINEERING_LANE.md](docs/CLAUDE_ENGINEERING_LANE.md) (2026-09-15);
  older whole-project instructions do not widen it. Nothing here overrides the 2026-09-21
  presentation-first plan or newer owner instructions.
- **Honest completion:** leave incomplete, unverified and blocked tasks unchecked; publish exact
  evidence for any completion.
- **Presentation and polish sessions** also consult [docs/NATIONALS_POLISH.md](docs/NATIONALS_POLISH.md)
  (game feel, presentation, Nationals polish).
- **Model style (owner, 2026-09-10):** every new or reworked model must look like it belongs to the
  cute blocky world, with no unnecessary detail or realistic anatomy or materials. Canonical:
  [docs/Art_Direction.md section 0](docs/Art_Direction.md#0--new-models-must-belong-to-tump). Read it for
  model, pet, skill/VFX geometry, prop and map work. Animation realism is weight, timing and contact
  inside this style. Better animation includes skill VFX, casting and separate animations for
  different actions; do not repeat animations unless necessary (same document, section 0.1).
- **Home screen, menu background, season or hero-showcase animation (owner, 2026-09-23):** first read
  [docs/HOME_SCREEN_ANIMATION_METHOD.md](docs/HOME_SCREEN_ANIMATION_METHOD.md): research first, the
  real models never drawn, faces never altered, re-time so every moment breathes, the review loop.
  Reuse the method, not the Zack loop's shots, timetable or gags. **Its section 0 is standing
  (2026-09-24): every beat breathes, the characters act out their personality, and the piece tells
  a story about TUMP.**
- **Wrap-up (2026-09-10):** verify and push, then a thorough continuation handoff in chat.
  Unfinished map work may be handed off; current map evidence is in the ledger and
  `docs/MAP_FINAL_PASS.md`.

Every ⚠️ below replaced something that actually went wrong. None is a style preference.

---

## 1 · What this repository is

The **Unity 6 build** of Tumbang Preso: 1st place at the Gear Up NCR Esports Game Dev Challenge and
NCR's entry at the nationals in General Santos City.

- ⚠️⚠️ **THIS REPO IS THE GAME. THE GODOT REPO IS FROZEN REFERENCE.**
  [DOST-GameDev](https://github.com/DOST-GameDEV/DOST-GameDev) may be read, quoted, ported from and
  cited; **never edit it, commit to it, or copy one of its files over the equivalent here.** Where
  they disagree, including a design document, this repo is current.
- ⚠️ `Design.md`, `Art_Direction.md` and `HUMAN.md` exist in both repos. **The live ones are in
  `docs/` here** (flattened from `docs/godot/` on 2026-08-23).
- ⚠️⚠️ **TWO MODES AND BOTH SHIP.** Classic is the street game with no powers. Hero Strike adds seven
  heroes with two skills and an ultimate each, for the competitive ceiling. Neither is a variant of
  the other (`docs/VISION.md` section 1).
- ⚠️ `Port_Ledger.md` section 12 records an ability layer being DELETED (eight verbs bolted onto the
  single game). That is not Hero Strike.

---

## 2 · How a session runs

### 2.1 Do the work. Do not narrate it.

- ⚠️⚠️ **No status reports mid-task.** A turn ends when prose replaces a tool call, so a "where
  things stand" summary IS stopping. Report once, at the end.
- ⚠️⚠️ **Never cite capacity** ("context limits" and the like). Never raise the subject.
- Do not stop to ask whether to continue work already asked for, or for permission a standing
  instruction covers (pushing, in particular).
- **The only legitimate stops:** genuinely finished, or truly blocked. When blocked, finish
  everything that is not blocked, then ask ONE specific question.
- ⚠️ Do not editorialise about deadlines or feasibility.
- ⚠️ Do not hand back work you can do. Scenes build from code (`Editor/SceneBuilder.cs`); matches,
  compiles, tests, probes, renders and builds all run from the command line. Hand back only a human
  judgement ("does this FEEL right") or a credential.

### 2.1b ⚠️⚠️ NEVER SIT AND WATCH A TEST RUN. RUN IT IN THE BACKGROUND AND KEEP CODING.

- A batchmode Unity launch is 3 to 12 minutes (a cold `Library` import longer). Start every
  `-runTests`, `-executeMethod`, build and long probe **in the background**, then start the next
  piece of work. Collect the verdict from `Logs/*.xml` when it lands (assert on the XML, section 7).
- The one exception is the LAST run before a build, where the verdict is the next step.
- ⚠️ `dotnet test` on `Core.Tests` is about 40 ms and needs no editor: run it in the foreground
  freely.
- ⚠️ **Do not edit `.cs` files while a Unity run is in flight**: it recompiles mid-run and the result
  describes neither version.

### 2.2 The shape of a session: WORK → BUILD → HANDOFF

1. **Do ALL the work** and verify it yourself (`dotnet test`, EditMode, PlayMode, probes).
2. **Build the .exe to the Desktop.** ⚠️⚠️ **Every build deletes the previous output first**
   (`GameBuilder.PurgeOutputDirectory` does it and refuses a path that is not a previous player).
   An incremental rebuild once kept a corrupted `level1`, and a reused launcher keeps its old
   timestamp. Section 7 has the manual procedure.
3. **Write the handoff in the chat reply, never as a file** (section 2.4).

- ⚠️⚠️ **"Task" means the whole request.** If any item is unfinished, keep working: do not build and
  do not come back.
- ⚠️ **Push automatically. Finished means pushed.** Every batch that compiles and passes goes up.

### 2.3 `docs/TODO.md` is the shared worklist. Tick it and add to it.

- ⚠️⚠️ **Every session reads it and updates it in the same commit as the work.** Check it before
  inventing a task.
- ⚠️⚠️ **Tick an entry off by moving it to [`docs/TODO_Archive.md`](docs/TODO_Archive.md) in the same
  commit, keeping its number.** TODO.md is the OPEN list (it once reached 22,930 lines and stopped
  being readable).
  - An entry's index row stays in TODO.md while its HEADING says `OPEN`, `IN PROGRESS` or `NOT DONE`
    (its body lives in `TODO_Backlog.md` since 2026-09-23). **Status goes in the heading, never in
    prose.**
  - ⚠️⚠️ **A session report is not an open item: write it, then archive it in the same commit.**
  - ⚠️ **Never delete or summarise an entry away.** The archive keeps whole bodies and numbers, and
    TODO.md keeps a one-line index row, so every `docs/TODO.md` section pointer still lands.
  - ⚠️ Section numbers are not unique (53, 63, 64, 65 repeat) and will not be renumbered: **search by
    title as well as number.**
- **Add anything you find and do not fix**, in the same shape: what is wrong, where, what done looks
  like. **Keep the numbers** ("40% of the arena" beats "too big").

### 2.4 The handoff contract

- ⚠️⚠️ **Never commit a handoff prompt as a file.** It goes in the chat reply.
- ⚠️⚠️ **Every handoff opens by pointing at the rules**, adapted from:
  > Read `CLAUDE.md` first, then `docs/VISION.md`, then `docs/TODO.md`. They carry the rules of the
  > repo, what the game is for, and what is open. Do not skip them because this prompt summarises
  > the task; the summary is not the rules.
- Then: repo and branch, exact HEAD, test and build state, what changed and what was measured, and
  what to pick up next **as a pointer to its `docs/TODO.md` entry**, not a copy.

---

## 3 · Writing, commits and comments

- ⚠️ **Never add a `Co-Authored-By` trailer, of any kind.** Sole-authored competition work.
- ⚠️ **Never mention Claude, Anthropic or any AI tooling** in commits, code comments, the README or
  anywhere in the repository.
- ⚠️ **No em dashes anywhere.** Rewrite the sentence rather than swapping the character.
- ⚠️ PowerShell here-strings break on embedded double quotes in `git commit -m`: write the message to
  a file and use `git commit -F`.
- **Comment the WHY, at length, in ⚠️-marked notes above the thing.** Record deletions and their
  reasoning. A measured number says so, and what it was measured against.

---

## 4 · Architecture invariants

Do not "improve" any of these; each replaced something that failed in play or in a probe.

- ⚠️⚠️ **`Packages/com.tumbangpreso.core/` never gets a `UnityEngine` reference** (`noEngineReferences`
  in its asmdef, no exceptions). It holds the match rules, scoring, trait tables, stamina, throw
  legality and combat geometry, every number measured, asserted in a second.
- ⚠️ **One copy, two toolchains:** the source lives in the package and
  `Core/TumbangPreso.Core.csproj` compiles those same files in place. Never "fix" this by copying.
- **Contact resolves by DISTANCE on the host, never a trigger volume** (16 of 36 overlaps failed to
  land).
- **Every point is awarded in ONE host-side function**, `MatchDirector.AddScore`.
- **The taya role is DERIVED**, `(round - 1) % 4`, never accumulated.
- **The box is a SQUARE**; X and Z clamp independently (2.9 m apart on the diagonal from a circle).
- **A bot presses the same buttons a human does** (`InputIntent`). AI never calls a gameplay method.
- **Stuns overlap via `Max()`, never additively** (the whole bound on stun chains in 1-vs-3).
- **Every impulse derives from `Friction`** as `v²/(2·Friction)`: write the distance, solve the speed.
- **Entry 0 of each prop list stays neutral** (what an unpicked prop wears).
- **One control, one action, per CONTEXT and per DEVICE** (since 2026-08-27 and 2026-09-02).
  Gameplay and spectating are separate contexts (a spectator has no body; `Rebinding.SpectatorContext`,
  `SpectatorBindingTests`, `docs/TODO.md` section 35.3). Two actions sharing a key inside one context
  is still a defect (`InputMapAndAbilityTests`). A control path already carries its device.
  `Rebinding.ResolveBindingIndices` returns every binding (reading only the first halved what the
  rule checked), and `TryRebind` writes to the binding of **the device just pressed**.
- ⚠️ **The camera is FPP *and* TPP (this was § 3a).** A Person is always FPP, a Prop always TPP
  (derived from `is_person`, asserted). Emotes swing to TPP and back, local only. Spectator is a
  fourth rig (free, follow, POV). The note "third person was a mistake" was wrong; the real mistake
  was an overhead follow camera.
- ⚠️ **Emotes end ONLY by interruption** (movement, a verb, losing the right to act), through
  `EmotePlayer.Stop()`, which is where `EndEmoteView` hangs. No timer; a clip-finished stop, if ever
  wanted, routes through `Stop()`.

---

## 4a · ⚠️⚠️ THREE DEVICES, EVERY TIME: MOUSE AND KEYBOARD, CONTROLLER, AND TOUCH

Owner: *"anytime we add a feature, make sure all controller and mobile is considered"*. **Any
feature, not just UI.** It is enforced by construction because checklists failed three times
(`docs/TODO.md` sections 96, 114, 124.11). **The list below is the authority; there is deliberately no
count in this sentence.**

- ⚠️⚠️ **A new `Verb` does not compile without a pad binding and a thumb target.**
  `InputLayer.InputCatalogue.For` is a switch with **no discard arm**; `Runtime/csc.rsp` makes CS8509
  an error, and every `VerbInput` field is a constructor parameter. **Do not add a `_ =>` arm; do not
  delete that `.rsp`.**
- ⚠️⚠️ **A new screen gets a focus path and thumb-sized hit areas by construction**:
  `MenuKit.BuildCanvas` and `ConvertedScreen.Start` both install `InputLayer.ScreenFocus`.
- ⚠️⚠️ **`InputSurfaceCheck` refuses a build whose source builds a Canvas outside the kit** (reads
  runtime sources as TEXT; in `Checks.RunAll`).
- ⚠️⚠️ **Screens back out through `InputLayer.MenuNav`; a keyboard literal is a test failure**
  (`ControllerSupportTests.EveryScreenBacksOutThroughTheOneReaderRatherThanAKeyboardLiteral`, reading
  sources as text; the old `Input.GetKeyDown(KeyCode.Escape)` in eleven places let a pad reach every
  screen and leave none). Three files may still read it and say why. ⚠️ `MenuNav` keeps the legacy
  `KeyCode.Escape` read on purpose: it is how Unity reports **Android's hardware BACK**. `docs/TODO.md`
  section 142.1.
- ⚠️ `InputSurfaceProbe` DISCOVERS screens (build settings and assembly) at nine desktop shapes, two
  phone shapes and the owner's short wide window (`ProbeResolutions`). ⚠️⚠️ `UiClickProbe` still
  hard-codes five screens: leave it, never copy it.

**What this asks of you:**
1. **A new verb:** answer pad and thumb, then run `InputAssetSync.Regenerate` (`InputContractTests`
   fails until you do).
2. **A new non-verb action:** add a `ScreenInputCatalogue` row. A `null` pad path is a legal, written
   answer (`ToggleFullscreen`); silence is not.
3. **A new screen:** build it through `MenuKit` or `ConvertedScreen`. A bare `Canvas` ships a screen
   a pad and a thumb cannot use.
4. **Any other feature:** ask out loud how it is reached on a pad, what a thumb presses, what the
   prompt says on each, and ⚠️ **how it is LEFT on a pad** (B backs out via `MenuNav`, Start opens
   pause). ⚠️ Prompts read the live binding through `Rebinding.DisplayNameFor(asset, action, device)`,
   never a literal.

- ⚠️⚠️ **Crossplay is a separate claim and input must never touch it.** `NetSession.ProtocolVersion`
  is the match FORMAT; no device information goes on the wire, so an input change never moves it
  (`InputContractTests.TheInputPassDidNotMoveTheProtocolVersion`). ⚠️ When it does move, rebuild the
  Windows and Android players from the same commit and ship together (`docs/FUTURE.md` section 15).
  UGS project `dcf0831e-a5f4-43b4-832e-b687f13a3569`, org `matthewtlabrador`; a machine on another
  project reads an empty lobby, not an error.
- ⚠️ **Matchmaking:** casual is one crossplay pool (`v21.Classic.Casual`, three parts); **ranked keeps
  all five parts and bands by device** (`MatchmakingRules.PoolKey`; `FUTURE.md` section 14: separate
  the pools instead of aim assist; `docs/TODO.md` section 130.4).
- ⚠️⚠️ **Before believing a crossplay report, check the phone:** `NetIdentity` once cached a FAILED
  sign-in for the process lifetime (section 130.2) and `Shader.WarmupAllShaders()` ANR'd Android
  before the menu (section 130.5).

---

## 5 · Design.md and the ledger

- **`docs/Design.md` is the balance source of truth:** a number in the code must match it, or one of
  the two is a bug. It describes **Classic only** (its section 13 lists what it does not govern).
- ⚠️ **Port from the GDScript and `Balance.cs`, never from the prose.** A new disagreement: fix the
  prose, note it in `docs/Design_Drift_Report.md`, and say so in the commit.
- **`docs/Port_Ledger.md` is the definition of done** for the port (CONVERTED / PARTIAL / MISSING per
  Godot script). Update the row; never report the port done while any row reads MISSING. A 26-line
  `kill_plane.gd` is still a feature the player meets.

---

## 6 · Art, models and iteration

- ⚠️ **The art is the team's own work**, built character by character (`tools/build_person_voxel.py`,
  `docs/Voxel_Person_Guide.md`; replacement queue in `docs/Port_Plan.md` section 8). Ask which pieces
  are final before treating any as disposable.
- ⚠️ **When new animations land, revisit `ModelImportSetup`.** Rigs import as **Generic** because the
  clips ship with their own rig; clips from a library (Mixamo) would make Humanoid right.
- ⚠️⚠️ **Sourced SFX are provisional until the owner hears them in play.** He rejected the can hit,
  can down and button hover, and their originals are restored. To restore another: read
  [`docs/Asset_Sourcing.md`](docs/Asset_Sourcing.md) section 5.5 and [`Attention.md`](Attention.md)
  section 13; old blobs are at `ee8bced^`; restore only the named target, resolve aliases, and move it
  from `tools/build_ability_audio.py` `REPLACEMENTS` to `KEPT` in the same commit. **Never roll back
  the whole asset pass for one cue.**
- ⚠️ **The IKE slipper carries the real Nike wordmark as geometry**; first in the replacement queue
  (`docs/Port_Plan.md` section 8 lists what a replacement must preserve).

### 6.0 ⚠️⚠️ DO NOT DECIMATE, COMPRESS OR RECOLOUR SOURCED ART TO SAVE PERFORMANCE. IT SHIPS AS DELIVERED

Owner: *"no need to lower triangles or compress dont worry it wont lag"* (after a 74,170-triangle
jeepney was decimated to 3,000, its 17 materials collapsed to 1, and it became a pink slab).

- ⚠️⚠️ **The budget is not the constraint.** A performance claim about sourced art is MEASURED first
  (`FrameRateHistogram` `MaxSeconds`, `LongFrames`) and quoted with the number.
- ⚠️⚠️ **Not a licence to reskin someone's model either.** Section 6.4's palette is for colours chosen
  in code.
- **Fair game:** import settings, shader, placement, size. **Not:** vertex count, texture resolution,
  material count, colours.
- ⚠️ `docs/Asset_Sourcing.md` section 7.1's "decimate it" is reversed by this rule.

### 6.1 Every model iteration gets a picture, and every picture gets a new filename

- ⚠️⚠️ **Show, do not describe.** Render after every iteration through the in-engine probe pipeline
  (never an external renderer: the toon shader, ink outline and linear colour are the look).
- The canonical outputs are the **4-angle turnaround** and the **cast lineup** (orbits deprecated).
- ⚠️⚠️ **Version every filename** (`zack_hair_v1.png`, `_v2`): chat clients cache by filename.
- **Force-reimport sub-assets before rendering** a rebuilt `.glb`.

### 6.2 ⚠️⚠️ THE STANDING BRIEF FOR EVERY SCREEN: INTUITIVE, EASY TO GET AROUND, NEVER OVERWHELMING

| Claim | Failing it looks like | Receipt |
|---|---|---|
| ⚠️⚠️ **Intuitive** | A control's effect cannot be predicted, or pressing does nothing | section 108: EQUIP with no `onClick`; a screen drawn under its opener |
| ⚠️⚠️ **Easy to navigate** | The thing cannot be found, or left | section 96: the hub's one door went unfound by the person who commissioned it |
| ⚠️⚠️ **Never overwhelming** | Everything at once, in one flat list | section 92 (*"20 shits at once"*), section 94.7 (seven readability faults, probes green) |

Before writing a screen and before calling it done, answer in one sentence each:
1. **What is the ONE thing on this screen?** Size, place and colour everything against it.
2. **What is the first press, and can the player guess it?**
3. **What is on screen the player does not need RIGHT NOW?** Collapse, move behind a header, or cut.
   ⚠️ A group closed by default with a one-line summary beats the rows always open.
4. **How do they get out, and is it one press?** Escape, innermost layer first.

- ⚠️ **The test for adding anything is what the player must hold in their head, not build cost**
  (`docs/FUTURE.md` section 0.5 rule 11b).
- Sections 6.2b, 6.2c and 6.3 are how this is met; none is visible to any probe.

### 6.2a Every screen gets designed, and the method is written down

- ⚠️⚠️ **A feature without a screen is not shipped, and "I added a row" is not a design.**
- **The method is [`docs/FUTURE.md`](docs/FUTURE.md) section 0.5b** for ANY screen: five questions,
  four ordering tools (position, size, weight and colour, space), what transfers from reference
  games, and four things a screen owes. Its per-phase table names each phase's one thing.
- ⚠️⚠️ **A green layout probe is not a good screen.** Take the picture.

### 6.2b ⚠️⚠️ THE FOUR WAYS A SCREEN SHIPS BROKEN AFTER BEING "RENDERED", AND THE CHECK FOR EACH

| Photograph | Why |
|---|---|
| ⚠️⚠️ **Every state**, not the first one built | The boot screen ships as `OpenAtBoot()` as well as `Open()`, and nobody had seen the first |
| ⚠️⚠️ **Over the real background**, never an empty scene | Scrims and panel alphas are tuned against what is behind (`UiRows.Band` 3.5 per cent against the lit street) |
| ⚠️⚠️ **At the shape he plays at** | `Fullscreen` is false; his short wide window is shorter than all nine probe resolutions |
| ⚠️⚠️ **With every always-on chrome live** | `PlayerNameplate` drew across a new code-built screen; ask "is anything on top of me" rather than keeping a list |

⚠️ **If a screen cannot be rendered, it does not ship open** (above all one that appears at boot).

### 6.2c ⚠️⚠️ FOUR QUESTIONS ABOUT EVERY RECTANGLE ON A SCREEN

| Ask | Rule |
|---|---|
| ⚠️⚠️ **What is this size measured AGAINST?** | Only the rectangle the player sees. `AspectSafeCanvas` scales on the SHORT axis (about 1920 units wide at 4:3, about 2250 on his window; `Expand` keeps it at least 1920), so a percentage is two widths. **Size against content and state the arithmetic** (section 100: 580 units, the form plus margins). |
| ⚠️⚠️ **Is this image fitted to the region it is SEEN in?** | Every image gets an explicit fit and an explicit parent: the visible region, not `_root`. `FitInParent` sizes against the PARENT. |
| ⚠️⚠️ **What is this dimming layer FOR?** | A scrim buys legibility over a live scene; it is not decoration. Ask what it protects before retuning. |
| ⚠️⚠️ **Is this width measured against the NARROWEST box?** | Size controls against 4:3, never 1920. `UiRows.Cap`: the value column is about **368 units at 4:3**. `MenuKit.Label` OVERFLOWS rather than wrapping (section 108: an arrow off screen at 1366x768). |
| ⚠️⚠️ **If I delete this, what else was it doing?** | Full-screen graphics also eat clicks; name the replacement blocker in the same commit. |

### 6.3 ⚠️⚠️ MOVING AROUND THE GAME IS ITS OWN DESIGN PROBLEM, AND THE UNIT IS THE JOURNEY

**Walk the journey out loud** ("I want to X" to "X is done", naming every press). More than three
presses, or one control that must be discovered, means the flow is the bug.

- ⚠️⚠️ **Every destination has a visible door, and a door looks pressable** (section 96).
- ⚠️⚠️ **Never add a second door to fix findability.** Fix the door or move it (section 92).
- **Escape backs out on every screen, innermost first** (`ConvertedScreen.CancelTarget`).
- **A control that acts reacts to the pointer; one that does nothing must not look pressable.**
- **A dead end is a bug.**
- ⚠️ **The escape from any gate is ONE press and never needs the network** (section 97).
- ⚠️⚠️ `UiClickProbe` proves nothing is covered; it cannot tell you a door nobody looks at is found.
  Watch a launch or ask.
- ⚠️ **Settings-shaped screens use `UiRows`** (no offsets anywhere; a hand-written Y offset is correct
  at one height and one aspect).

### 6.4 ⚠️⚠️ NEVER USE BLUE OR NAVY ANYWHERE IN THE UI. NOT OUTLINES, NOT FILLS, NOT BACKGROUNDS, NOT GREYS WITH A BLUE CAST

Owner, repeatedly: *"i dont want to see blue shit"*, *"hey i said i dont want blue or navy"*. The first
version of this rule said "outlines", and the blue on screen was not an outline: `UiTheme.Ink` (a
navy that outlined every menu type style; now `1c0f06`), a slate character-select backdrop, navy
`MatchResult` and `RoleSwapCard` scrims, blue-cast `Panel`/`Card` greys, and
`GameBuilder.ConfigureSplash` rewriting a navy splash every build.

- **The rule, stated wide:** no blue, navy or cold grey in any UI colour or layer (outlines, fills,
  panels, scrims, rings, gradients, glyph tints, disabled states). **If a hex has more blue than red,
  it does not belong in a menu.**
- ⚠️ **A colour set in `ProjectSettings.asset` alone is not set:** `ConfigureSplash` overwrites it on
  every build. Both places or neither.
- ⚠️⚠️ **The palette is the logo's (2026-09-03, final).** `docs/TODO.md` section 133.1;
  role table in `docs/Front_End_Design.md` section 4.

  | Name | Hex | Logo share | Its ONE role |
  |---|---|---|---|
  | Deep red | `#980715` | 34.3% | The **outline** everywhere, and the one destructive control. Never a ground. |
  | Honey Quartz | `#FCD39F` | 23.1% | The **ground** of every light screen; base of the paper ramp |
  | Chartreuse | `#D6CE01` | 17.0% | The **action**: one per screen, the primary only |
  | Persimmon | `#FD8041` | 5.7% | The **marker**: the one value or selection that matters |
  | Golden | `#F5B521` | 4.2% | The front end's gold. ⚠️ `UiTheme.Amber` stays `#FFBA00` (the HUD reads it 15 times) |
  | Rim red | `#C32E0D` | 3.8% | The lit state of the deep red |
  | Army | `#B3A828` | 1.4% | The **dark ground**, the only one (the fighter picker's stage) |
  | ~~Khaki~~ | ~~`#E8C77E`~~ | not in the artwork | ⚠️⚠️ Deleted 2026-09-04: derived, not measured, zero call sites. Measure or derive at the call site with the arithmetic written down. |

- ⚠️⚠️ **Every value was measured by `tools/read_brand_palette.py`**, which agreed with itself across
  `tump_logo_colour.jpg` and `tsinelas_hit.jpg`. **Re-run it for a new logo**; it merges JPEG
  chroma-subsampled values.
- ⚠️ **Paper ramp: one colour at four tints** (`Paper #FEEBD4`, `PaperWarm #FDDFBA`,
  `PaperEdge #FCD39F`, `PaperSunk #DEBA8C`). Ink is a mix of the two darkest brand colours (red text
  reads as an error): `#55290F` at **10.5:1** and `#97491B` at **5.5:1** on the page, computed by
  `scratchpad/fontsrc/ramp.py`.
- ⚠️ **The carved wood is the old palette and is kept:** `#31190B`, `#5A2F14`, `#8B5227`, `#1D0E06`,
  cream `#F5E6C8`, amber `#FFBA00`, warm ink `#1C0F06`. `PaperPurityProbe.WoodFills` lists them to
  DETECT leftovers, and the in-match HUD is drawn in them on purpose (section 133.4).
- ⚠️⚠️ **The one exemption is gameplay: `UiTheme.Defense` (`0080e8`) means "the taya"**, opposite
  `Offense` orange. Never menu chrome (`ChatAndLobbyChromeTests` asserts it for lobby nameplates).
- ⚠️ **His authored pennant art is not this** (PLAY green, SETTINGS yellow, TUTORIAL blue, QUIT red).
  `VISION.md` section 6: his UI art IS the design system. Do not repaint it.
- ⚠️ **Check by grepping, not looking:**
  `grep -rnE 'Hex\("[0-9a-f]{6}"\)' Assets/TumbangPreso/Runtime/UI/UiTheme.cs` and read the third channel.
- ⚠️ A rule written against the one place a fault was seen does not cover the constant that caused
  it. (This section once sat between 6.3's heading and body; keep 6.3 whole.)

### 6.5 ⚠️⚠️ THE FRONT END IS DRAWN IN HIS ART'S OWN GEOMETRY, AND `WoodCraft` IS WHERE THAT LIVES

Owner: *"everything feels repetitive bcz i think u use the same code to generate them all"*, *"make
sure all ui isnt generated in the same way but follows a central theme"*. The palette was never the
problem; the SHAPE was. His surfaces are chamfered or rounded slabs with a BRIGHT keyline outside a
DARK rim over a full-height gradient with a varnish band a quarter down; code drew flat rounded
rectangles with dark outlines.

- **`Runtime/UI/WoodCraft.cs` is the transcription; its header carries the measurements.** Read it
  before drawing a surface. One base colour generates a whole control (ratios as HSV multipliers).
- ⚠️⚠️ **The paper front end's primary is `PaperCraft.Surface.Action`** (2026-09-02), the one chamfer
  on a cream screen; `docs/TODO.md` section 121.1 has the measurement against the old wooden halo.
  Its `Accent` is a closed list of two authored fills (his green; the lobby's brown).
- ⚠️⚠️ **`PaperKit.MakeAction` switches off every child graphic**, including `ArrowButtonView`'s
  `Artwork`, `Lit` and `Rim`. Disabling a component does not remove the objects it made.
- ⚠️ **Pick a role, not a fill:** `WoodCraft.Surface` is a closed list (`Button`, `Action`, `Panel`,
  `Header`, `Field`, `Paper`, `PaperField`, `Slate`).
- ⚠️ **A chamfer means pressable; a round means furniture.**
- ⚠️⚠️ **Cream and asphalt are SURFACES**, built without keyline, ramp or bevel (Paper and Slate).
- ⚠️ **`WoodSkin` or the sprite is wrong:** slabs slice horizontally only, so the component watches the
  rect; `GodotButton` and `GodotPanel` carry the same watch.
- ⚠️ **Nothing here repaints his art** (pennants, `BUTTON LONG`, `JOIN BUTTON`, arrows, key art).
- ⚠️⚠️ **Green is his primary, and that is evidence:** `UiTheme.MenuGreenFace` is the measured peak;
  `MenuGreen` `21a131` is a third darker than any pixel in his button.
- ⚠️⚠️ `childForceExpandHeight` silently overrides every `LayoutElement` under it, and a tint multiplies
  the sprite's own alpha: all six fixes in section 117.7 were invisible in source and obvious in a
  render. **Take the picture, then take it again.**
- `docs/CANONICAL_RENDERING_PIPELINE.md` has the render commands and five pitfalls; its "MANDATE FOR
  ALL AGENTS" heading belongs to another tool. Where it disagrees with this file, this file wins.

---

## 7 · This machine

| | Windows | Mac (since 2026-09-04) |
|---|---|---|
| Unity | `C:\Program Files\Unity\Hub\Editor\6000.5.8f1\Editor\Unity.exe` | `/Applications/Unity/Hub/Editor/6000.5.8f1/Unity.app/Contents/MacOS/Unity` |
| Modules | Windows Standalone, WebGL, Linux Dedicated Server | ⚠️⚠️ MacStandaloneSupport and WebGLSupport ONLY: `BuildMac`, never `BuildWindows` |
| dotnet | `C:\Program Files\dotnet\dotnet.exe`, SDK 9.0.317 | ⚠️⚠️ NOT INSTALLED: use the EditMode suite for the same numbers |
| python | `python` (3.12 on the `Matthew` profile), else `%LOCALAPPDATA%\Programs\Python\Python312\python.exe` | `python3` (3.14); bare `python` is not on PATH |
| RAM | 16 GB (it read 8 GB until a boot cap was cleared; re-check before blaming Unity) | |

- ⚠️ **Two Windows profiles:** `GameBuilder.BuildWindows` writes to this profile's Desktop
  (`C:\Users\matth\Desktop` or `C:\Users\Matthew\Desktop`). Verify the .exe exists and report its path.
- ⚠️⚠️ **The Python harnesses find Unity per machine** (`tools/qualify.py`, `tools/playmode_suite.py`,
  `tools/bot_sweep.py`) and pick `-buildTarget` (`OSXUniversal`, `Win64`). `qualify.py` says when the
  target is not the nationals one, and `--stage core` reports a missing dotnet instead of failing.
  `tools/cold_start.py` resolves the player and profile per platform, including the macOS bundle
  (`Contents/MacOS/TumbangPreso`, `Contents/Resources/Data/StreamingAssets`) and
  `~/Library/Application Support/BH Studios/Tumbang Preso`.
- ⚠️ **`PYTHONIOENCODING=utf-8` is required** on Windows or `audit_audio_reach.py` dies mid-output.

**Commands:**

```bash
dotnet test Core.Tests/TumbangPreso.Core.Tests.csproj
"/c/Program Files/Unity/Hub/Editor/6000.5.8f1/Editor/Unity.exe" -batchmode -runTests -nographics -projectPath . -testPlatform EditMode -testResults Logs/tests.xml -logFile Logs/tests.log
python tools/playmode_suite.py --gate            # THE PlayMode gate: every group, one verdict
python tools/playmode_suite.py --gate --twice    # the nationals gate: green twice, same shape
python tools/playmode_suite.py --plan            # print the partition, run nothing
"/c/Program Files/Unity/Hub/Editor/6000.5.8f1/Editor/Unity.exe" -batchmode -projectPath . -executeMethod TumbangPreso.EditorTools.Checks.RunAll -logFile Logs/checks.log
"/c/Program Files/Unity/Hub/Editor/6000.5.8f1/Editor/Unity.exe" -batchmode -quit -projectPath . -executeMethod TumbangPreso.EditorTools.GameBuilder.BuildWindows -logFile Logs/build.log
"/c/Program Files/Unity/Hub/Editor/6000.5.8f1/Editor/Unity.exe" -batchmode -runTests -projectPath . -testPlatform PlayMode -testCategory "!WallClock;!ThumbFloor" -testFilter "TumbangPreso.PlayTests.SteeringTests;TumbangPreso.PlayTests.CarryTests" -testResults Logs/targeted.xml -logFile Logs/targeted.log
```

- ⚠️⚠️⚠️ **A single-process PlayMode run is NOT the gate. Run `tools/playmode_suite.py --gate` and quote
  its number.** One process leaks objects, scenes and sessions across fixtures: four runs gave 42, 41,
  56 and 50 failures with the red SET changing (a SETTINGS case once failed with `CarryTests`' message).
  Groups are isolation boundaries, never exemptions (`docs/TODO.md` section 126.8d): every fixture runs
  in exactly one group, the partition is discovered from source, `--plan` refuses gaps or overlaps,
  and a filter typo fails. Section 143.1.
- ⚠️⚠️ **`-testCategory "!WallClock"` is part of the command:** `AiDiagnosticProbe` runs about 80 real
  seconds and fails on a busy machine (section 6). `[Explicit]` does not exclude in batch mode. Run
  it on purpose with `-testCategory "WallClock"`.
- ⚠️⚠️ **`!ThumbFloor` is a shrinking gap, not a flake:** `InputSurfaceProbe.TheFrontEndMeetsTheThumbFloor`
  read 1519 under-144-unit measurements and reads 50 near misses now (sections 126.2, 126.12);
  `ScreenFocus.MakeRoomForThumbs` gave the padding somewhere to grow, and
  `EveryScreenHasAFocusPathAndReachableTouchTargets` still passes. **Run that probe ALONE** (it loads
  every scene and overlay; section 126.8), on purpose, with `-testCategory "ThumbFloor"`; its failure
  message is the worklist and `Logs/input-surface.txt` the full sweep.
- ⚠️⚠️ **PlayMode takes NO `-nographics`**: it crashes the editor (`NullGfxDevice`), writes no XML, and
  exits 0.
- ⚠️⚠️ **Assert on the `.xml`, never the exit code.** ⚠️⚠️ And read `total` and `failed`, not `result`:
  `result="Passed" total="0"` happens when something destroyed the runner's objects
  (`PlayModeWorld.NeverTouch` guards it) or when **`-testFilter` was comma-joined: it is
  SEMICOLON-separated** (section 126.8c).
- ⚠️ `-batchmode -quit` exits before compiling scripts and returns 0: use `-executeMethod` or
  `-runTests` for a real compile.
- ⚠️ Launch Unity with `Start-Process -Wait -PassThru`, not `&` (`$LASTEXITCODE` empty, log sometimes
  missing).
- ⚠️⚠️ **A stale `Temp/UnityLockfile` looks exactly like a broken install** (2026-08-26: the package
  manager answered `path ... Received undefined` on every launch). Leftover `Unity.ILPP.Runner`,
  `UnityPackageManager` and `UnityShaderCompiler` processes can hold the lock. **Check that file
  first**, whether or not Unity is running; `rm Temp/UnityLockfile` fixed it.
- ⚠️ Bash heredocs are unreliable on Windows: write the script to a file and run it.
- **Editor checks run in one launch via `Checks.RunAll`**; the list in section 7.1 is the authority, never
  a count (if a number matters, count `Checks.Execute`'s own array).

**The version label (reversed 2026-08-28, owner: *"pls replace the version number to 1.00"*):** the
bottom-right corner shows the version again on every branch. `GameVersion.DisplayString` is the one
line that decides; the branch-name machinery is kept, so returning to it is one line if two .exe
files ever become indistinguishable. For reference:
- `GameBuilder.StampBuildBranch` writes `Resources/BuildBranch.txt` on every build; it is gitignored
  on purpose (per-build, per-branch). Absent or empty means "show the version".
- ⚠️⚠️ **Never put the branch name on the wire.** `Application.version` goes into the LAN beacon, the
  lobby record and the connection hello
  (`TheBranchNameNeverReachesTheVersionTheWireCompares`).
- ⚠️ `GameVersion.ApplyTo` widens the label and switches to Overflow (legacy `Text` wraps silently).

**Clean rebuilds:** Unity can rewrite `TumbangPreso_Data` while reusing the byte-identical launcher, so
the .exe keeps an old timestamp (a 15:03 build looked like the 14:34 one). ✅
`GameBuilder.PurgeOutputDirectory` deletes the output folder on every build and fails rather than
half-overwriting a locked folder (usually the game still open). When asked to rebuild or clean
rebuild, or when a timestamp is questioned:
1. Make sure no `TumbangPreso` or Unity process holds the output.
2. To KEEP the old player, move `Desktop\TumbangPreso-Unity` to a named backup first.
3. Run `GameBuilder.BuildWindows`.
4. Check the new `TumbangPreso.exe` **and** `TumbangPreso_Data` timestamps, launch that exact exe, and
   keep the backup until it passes. `SUCCEEDED` in the log proves a build, not that every file is new.

### 7.1 Verify by measuring

- `Core.Tests` asserts every balance number in about a second.
- `BotBehaviourProbe` runs a whole match in both modes on Eskinita and Ilalim ng Tulay (throws,
  retrievals, tags, skills, ultimates, penalties, emotes, hops). ⚠️ **It is seeded: never change the
  seed to make a run pass.** ⚠️⚠️ **Its numbers are liveness floors, never comparisons at n = 1**: at
  a fixed 1/60 s step eight matches spread 58 to 100 throws. `docs/TODO.md` section 10 said solved;
  section 16 measured otherwise and carries the noise floor and how many runs an A/B needs.
  `TwoIdenticalMatchesLandInsideTheNoiseFloor` (six minutes, `WallClock`) asks whether it is still
  honest. ⚠️ Reports before 2026-08-26 are three seats' worth (`GameLaunch.SoloSeat` defaults to 1),
  and ⚠️⚠️ before 2026-08-27 seat 0 steered in a rotated frame (section 34); do not compare them with
  newer ones.
- `AiDiagnosticProbe`: one round at 1x with every decision written out (`WallClock`).
- `AspectRatioProbes` drive real layout through nine resolutions.
- `SceneScriptCheck` refuses a build scene holding a component the PLAYER cannot bind. ⚠️⚠️ It reads
  scenes as TEXT because opening a scene hides the fault (a shipped build crashed with every other
  check green). `GameBuilder` runs it before every build.
- `MapGeometryCheck` refuses floating or buried props, floor holes and furniture in the defender's
  box (it found six faults on a signed-off map).
- `SceneDependencyCheck` OPENS every build scene and asserts references resolve, no missing scripts,
  a camera on every UI scene. ⚠️⚠️ **The opposite technique to `SceneScriptCheck`; never merge them.**
  ⚠️ It never saves a scene. On `54924fc`: 9 scenes, 11,536 components, 0 findings.
- **`Checks.RunAll`** runs `HeadlessCheck`, `ArenaCheck`, `MapGeometryCheck`, `AudioCueCheck`,
  `SceneScriptCheck`, `InputSurfaceCheck`, `SceneDependencyCheck` and `ShaderWarmupCollection` in one
  launch and runs all of them even after one fails. ⚠️ `ShaderWarmupCollection` REGENERATES the
  collection the loading screen warms from, and `GameBuilder` rebuilds it every build (both places
  or neither; section 130.5).
- `AbilityShowcaseProbe` photographs ability transients and fails a frame more than 12 per cent blown
  to white (VISION section 2 rule 5; it found Zack's ultimate at 62.8 per cent).
- **The `tools/audit_*.py` audits read source as TEXT and exit non-zero.** `ls tools/audit_*.py` is the
  list (no count here on purpose). Try `python` first (section 7 table).
  - `audit_ability_authority.py`: every ability call that moves a body or writes score must be
    HOST-ONLY behind `NetAuthority.ShouldResolve()` (section 25.1).
  - `audit_request_call_sites.py`: wire entry points nothing calls; tests do not count (sections 38.3,
    38.5).
  - `audit_wire_payloads.py`: each message's writer and reader, field by field (section 38.6).
  - `audit_audio_reach.py`, `audit_presentation_reach.py`: whether a cue or effect reaches every peer.
    ⚠️ The first once counted a gate mentioned in a comment; it strips comments now.
  - `audit_cue_relay.py`: cues relayed AND played locally (double-fire). ⚠️⚠️ Read its header first;
    the gate is usually in the CALLER, and its `WRAPPED` and `OWNER_DRIVEN` allowlists assert their
    evidence line still exists.
- **`tools/net_link.py` and `tools/net_matrix.py`** put two real players on a controlled link
  (section 137). ⚠️⚠️ **Never `UnityTransport.SetDebugSimulatorParameters` or `DebugSimulator`**: both are
  `[Obsolete]` with no effect in netcode 2.13.1 (the simulator stage needs the
  `UNITY_MP_TOOLS_NETSIM_IMPLEMENTATION_ENABLED` define from `com.unity.multiplayer.tools`, which is not
  in the manifest), and a table built on them measures a perfect link.
- `tools/` also holds player-side capture scripts.
- Measuring beats playing: one session found a HUD string rebuilt per frame costing an eighth of the
  frames, a slipper resting 0.7 m outside the wall, and an unseeded probe reading 110 then 467 penalties.
