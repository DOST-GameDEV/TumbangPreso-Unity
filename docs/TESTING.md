# How to test this

**Two gates. Pay the fast one for every change and the full one before a build or for anything
that touches gameplay.** `docs/TODO.md` § 7 is the reason this page is shaped this way: 🧑,
2026-08-25, *"we have too many tests and we are wasting so many credits to run them all"*. The
cost was never the assertions, it was the **Unity launches** (each pays the editor start, the
asset database and a script compile), and a full pass used to be seven of them. It is three.

---

## 0 · The two gates

**FAST GATE. Every change.** One second plus two Unity launches.

```bash
dotnet test Core.Tests/TumbangPreso.Core.Tests.csproj --nologo
```

```bash
"/c/Program Files/Unity/Hub/Editor/6000.5.8f1/Editor/Unity.exe" -batchmode -projectPath . -executeMethod TumbangPreso.EditorTools.Checks.RunAll -logFile Logs/checks.log
```

```bash
"/c/Program Files/Unity/Hub/Editor/6000.5.8f1/Editor/Unity.exe" -batchmode -runTests -nographics -projectPath . -testPlatform EditMode -testResults Logs/edit.xml -logFile Logs/edit.log
```

**FULL GATE. Anything touching gameplay, and before every build.** The fast gate plus:

```bash
"/c/Program Files/Unity/Hub/Editor/6000.5.8f1/Editor/Unity.exe" -batchmode -runTests -projectPath . -testPlatform PlayMode -testCategory "!WallClock;!ThumbFloor" -testResults Logs/play.xml -logFile Logs/play.log
```

⚠️⚠️ **PlayMode has NO `-nographics` and adding it CRASHES the editor**, not the tests. Unity
selects `NullGfxDevice` and the first offscreen camera dies inside it; the run writes no `.xml`
and still exits 0. This page carried the flag on that line for months.

⚠️⚠️ **ALWAYS ASSERT ON THE `.xml`, NEVER ON THE EXIT CODE.** Both that crash and a genuine
failure come back as 0.

⚠️⚠️ **AND AS OF 2026-09-03 THE FULL PLAYMODE RUN IS NOT A RELIABLE GATE ON ITS OWN. READ
`docs/TODO.md` § 126.8 BEFORE QUOTING A NUMBER FROM IT.** Two runs of nearly the same code, an hour
apart, came back 42 red and then 41 red **with eleven suites going green and eleven different ones
going red between them.** The stack traces are `MissingReferenceException` inside the tests and
"the arena built no SliceRunner" style assertions: objects and scenes outliving the test that made
them. The same nine suites that produced about twenty failures in the full run produced **two** when
run together on their own, in 105 seconds. **A gate whose red set moves is not measuring the code.**

⚠️ **Until § 126.8 is closed, verify a change with a `-testFilter` over the suites it touches**, and
treat the full run as a survey rather than as a pass or a fail. Every green PlayMode number in the
handoffs (§ 94.8's *"targeted: 15/15"*, § 125's *"`InputSurfaceProbe` 5/5"*) is a targeted run and
every one of them is honest; the suite only comes apart when it is run as one process.

---

## 1 · The rules, in one second, no Unity at all

```bash
dotnet test Core.Tests/TumbangPreso.Core.Tests.csproj --nologo
```

**Expect 67 passed** (2026-08-26).

This is the balance layer: match rotation, scoring, traits, stamina, throw legality, the hit
window, the combat geometry, the trip and mash arithmetic, and the rematch vote's counting rules.
Every number here was reproduced from the measurements recorded in `Design.md`, so **if this goes
red, the tuning changed**, and that is the one thing in this port that must never change by
accident.

⚠️ **This is where a new rule belongs if it can live here at all.** `CLAUDE.md` § 4: the package
must never acquire a `UnityEngine` reference, and being engine-free is exactly what lets these run
in a second instead of behind a two-minute editor launch.

---

## 2 · The two suites, headless

```bash
"/c/Program Files/Unity/Hub/Editor/6000.5.8f1/Editor/Unity.exe" -batchmode -runTests -nographics -projectPath . -testPlatform EditMode -testResults Logs/edit.xml -logFile Logs/edit.log
```

**Expect 295 passed** (2026-09-03, in about 6 seconds). ⚠️ It read *"expect 121"* dated 2026-08-26
until then, which is a number nobody could have used: a reader running it today would have seen 295
and had no way to tell whether that was healthy growth or a duplicated fixture. **A stale expected
count is worse than none**, because it invites the reader to go looking for the difference. Wiring,
the arena bounds, reconnection, seat reclaim, leader
election, join codes, names, emotes, the map grades, the dead-feature audit and the HUD's
measured layout.

```bash
"/c/Program Files/Unity/Hub/Editor/6000.5.8f1/Editor/Unity.exe" -batchmode -runTests -projectPath . -testPlatform PlayMode -testCategory "!WallClock;!ThumbFloor" -testResults Logs/play.xml -logFile Logs/play.log
```

**155 cases are collected** (2026-09-03; the line here read *"expect 62 passed"* dated 2026-08-26).
⚠️⚠️ **THERE IS NO HONEST PASS COUNT FOR THIS SUITE TODAY AND `docs/TODO.md` § 126.8 IS WHY.** Two
runs an hour apart came back 113 and 114 passed with a different red set each time, so a number
written here would be a number the next reader could not reproduce. **Fix § 126.8 before putting a
count back.** This one actually **runs the game**: three whole matches
(Classic and Hero Strike on Eskinita, Hero Strike on Ilalim ng Tulay), the taya rotating through
every seat, bodies staying on the ground, the box holding the taya, and sprinting draining to
fatigue in the time the arithmetic says it should.

Two of those are new on 2026-08-26 and both were written to answer a report about a PICTURE:

* **`TrainingStreetProbe`** walks the guided tutorial lesson by lesson and writes down every
  renderer within two metres of the eye with its viewport position and its WORLD SIZE, plus every
  tsinelas with its state, its holder and its clearance off the road (`Logs/training-street.txt`,
  `Logs/training-lessons.txt`). ⚠️ **Use it before reasoning about any screenshot of the
  tutorial.** Three of the four things 🧑 photographed on 2026-08-26 were objects nobody could
  name from the pixels, and one run of this named all of them: a 2 m amber ball where a ground
  ring was meant to be, a loose tsinelas riding a hidden seat's hand, and a pet whose owner had
  been switched off. `docs/TODO.md` § 15.
* **`SettingsScrollProbe`** opens the settings list at the same nine resolutions
  `AspectRatioProbes` uses and asserts the scrollbar is inside the panel, that no rebind row is
  drawn underneath it, and that the list can be scrolled from end to end
  (`Logs/settings-scroll.txt`). ⚠️ Its first version compared WORLD corners and printed zero for
  every column: on a canvas rendering to a camera every element sits within a hair of the same
  world x, so it passed nine resolutions while measuring nothing. It converts into the canvas's
  own space now, the way `AspectRatioProbes.AssertInside` does.

### The WallClock category

⚠️⚠️ **`-testCategory "!WallClock"` IS NOT OPTIONAL AND IT IS NOT A SPEED TRICK.**
`AiDiagnosticProbe` runs a round at **1x for about 80 real seconds** on purpose, so its result
depends on how busy the machine is: it has failed at 21.6 s, 29.9 s and 37.6 s against a 20.0 s
bound and passed on immediate re-runs with nothing changed. A red result from it in a default run
carries no information and costs the next session a full suite to learn that again.
`docs/TODO.md` § 6 has the decision and the evidence.

⚠️ **`[Explicit]` does not do this in batch mode.** It was tried; the run still reported both
tests. The exclusion has to be on the command line.

⚠️⚠️ **THE CATEGORY MEANS "EXCLUDED FROM THE DEFAULT RUN", AND ITS TWO MEMBERS ARE EXCLUDED FOR
DIFFERENT REASONS.** `AiDiagnosticProbe` is excluded because it is REAL-TIME and its result
depends on the machine. `BotBehaviourProbe.TheOverclockWindowSweep` is excluded because it is
LONG: four whole Hero Strike matches, about twelve minutes, and `docs/TODO.md` § 7 is an entry
about the suite already costing more than it returns. Neither is optional to run when the thing
it measures is what you changed.

Run them on purpose, when somebody is going to read the report:

```bash
"/c/Program Files/Unity/Hub/Editor/6000.5.8f1/Editor/Unity.exe" -batchmode -runTests -projectPath . -testPlatform PlayMode -testCategory "WallClock" -testResults Logs/ai.xml -logFile Logs/ai.log
```

Read the counts out of the XML:

```bash
powershell -c "$x=[xml](gc Logs/play.xml); $x.'test-run'.passed + '/' + $x.'test-run'.total"
```

### The ThumbFloor category

⚠️⚠️ **THE SECOND EXCLUSION, AND IT IS A KNOWN GAP RATHER THAN A FLAKE.**
`InputSurfaceProbe.TheFrontEndMeetsTheThumbFloor` measures every menu control against the 144-unit
touch target floor (`TouchMetrics.MinTargetUnits`). The front end was authored for a mouse, so when
the category was added on 2026-09-02 it reported **1519 measurements under the floor across twelve
shapes**: rebind keycaps at 428x46, sliders at 344x34, the main menu's pennants at 228x60.

⚠️ **IT IS EXCLUDED RATHER THAN DELETED, AND EXCLUDED RATHER THAN LEFT FAILING**, for the two
reasons `CLAUDE.md` § 7 gives: a known gap with no test is a gap that gets forgotten, and a
permanently red test teaches people to skim results exactly as a falsely green one does.

```bash
"/c/Program Files/Unity/Hub/Editor/6000.5.8f1/Editor/Unity.exe" -batchmode -runTests -projectPath . -testPlatform PlayMode -testCategory "ThumbFloor" -testResults Logs/thumb.xml -logFile Logs/thumb.log
```

**The failure message is the worklist**, and the full report is written to
`Logs/input-surface.txt` whichever way the test goes. ⚠️ **Read that file rather than only the
count**: it names each shortfall's screen, control and resolution, and it also prints the scrollbars
it EXEMPTED, which is a decision (a scrollbar is dragged, not pressed) recorded where it is made
rather than in a comment somewhere else.

⚠️ `docs/TODO.md` § 126.2 is the layout pass that answers it. **A run whose report says *"the camera
was replaced part way through the sweep"* measured fewer shapes than usual on that scene**, so
compare reports by what they covered and not only by the count at the bottom.

⚠️ **If a run produces no log at all, Unity never started.** It leaves child processes holding
the project lock after it exits. Kill `Unity`, `Unity.ILPP.Runner` and `UnityPackageManager`,
delete `Temp/UnityLockfile`, then try again. An empty log is not a passing run.

⚠️⚠️ **A STALE `Temp/UnityLockfile` LOOKS EXACTLY LIKE A BROKEN INSTALL.** On 2026-08-26 one made
the package manager answer `path ... Received undefined` on every launch, including against an
empty project, and the session before it concluded Unity itself was broken machine-wide. Deleting
the file fixed it outright. **Check it before believing anything worse.**

---

## 3 · The five checkers

⚠️⚠️ **RUN THEM AS ONE LAUNCH. THE INDIVIDUAL COMMANDS BELOW ARE FOR WHEN YOU WANT ONE REPORT,
NOT FOR A VERIFICATION PASS.**

```bash
"/c/Program Files/Unity/Hub/Editor/6000.5.8f1/Editor/Unity.exe" -batchmode -projectPath . -executeMethod TumbangPreso.EditorTools.Checks.RunAll -logFile Logs/checks.log
```

`Checks.RunAll` runs `HeadlessCheck`, `ArenaCheck`, `MapGeometryCheck`, `AudioCueCheck` and
`SceneScriptCheck` in one editor start and exits non-zero if any fails. It runs all five **even
after one fails**, because stopping at the first is how a session fixes one thing, relaunches,
finds the second, and pays the start-up cost five times over. `Logs/checks.txt` says which one
went red; each check still writes its own report beside it.

Each writes a readable report into `Logs/` and exits non-zero on failure.

```bash
"/c/Program Files/Unity/Hub/Editor/6000.5.8f1/Editor/Unity.exe" -batchmode -quit -nographics -projectPath . -executeMethod TumbangPreso.EditorTools.MapKit.ArenaCheck.Run -logFile Logs/arena.log
```

`Logs/arena-check.txt`. The `floorcheck.py` replacement: five bounds, and it **aborts rather
than warns**. Bound 3 is the one nobody had written down, and it currently sits **exactly** on
its limit at 8.60 against a wall face of 8.60. **If you grow the box, this is what tells you.**

```bash
"/c/Program Files/Unity/Hub/Editor/6000.5.8f1/Editor/Unity.exe" -batchmode -quit -nographics -projectPath . -executeMethod TumbangPreso.EditorTools.MapKit.MapGeometryCheck.Run -logFile Logs/mapgeom.log
```

`Logs/map-geometry-check.txt`. Opens each map and measures four things: every renderer rests on
something (or carries an `AirborneByDesign` with a printed reason), nothing solid taller than
`stepOffset` stands inside the chalk, nothing is within 1.4 m of where the can spawns, and a
0.5 m grid across the walled area has ground under every sample.

⚠️ **It gates Ilalim ng Tulay only.** Eskinita and Bayan Plaza are imported `.tscn` scenes and
are reported on rather than gated, because gating them today would mean either fixing two
imported scenes in one pass or switching the check off, and the second of those is how a check
dies. Their findings are `TODO.md` § 4.

⚠️ **A float finding prints the level profile under the prop** (`0.212 x5, 0.150 x20`). The
coverage rule is the part that is easy to get wrong, and arguing with it from the source instead
cost a round trip. That profile is also what caught all four utility poles yawed backwards, with
twenty of the twenty-five squares under each one sitting over the back lots.

```bash
"/c/Program Files/Unity/Hub/Editor/6000.5.8f1/Editor/Unity.exe" -batchmode -quit -nographics -projectPath . -executeMethod TumbangPreso.EditorTools.AudioCueCheck.Run -logFile Logs/audio.log
```

`Logs/audio-cue-check.txt`. Checks cues against files **in both directions**, plus magic bytes.
It currently reports the five `ability_*` sounds as known-dead, which is correct: they ship for
a system that was deleted.

```bash
"/c/Program Files/Unity/Hub/Editor/6000.5.8f1/Editor/Unity.exe" -batchmode -quit -nographics -projectPath . -executeMethod TumbangPreso.EditorTools.SceneScriptCheck.Run -logFile Logs/scenescript.log
```

`Logs/scene-script-check.txt`. Fails any scene in the build settings holding a `MonoBehaviour`
the player cannot bind to a script: an inline `MonoScript` stub, an `m_Script` with no guid, or
a guid resolving to nothing. Scenes outside the build settings are reported, not gated.

⚠️⚠️ **THIS IS THE ONLY CHECK THAT CAN SEE THIS CLASS OF BUG AND IT WAS WRITTEN AFTER ONE
SHIPPED.** The 2026-08-25 build hard crashed the moment a player selected Ilalim ng Tulay, with
"The file 'level8' is corrupted!" and "[Position out of bounds!]" in the player log. Nothing was
corrupt: every serialized file parsed clean. Eight `HazardVolume` components referenced a stub
because the class was declared inside `HazardMap.cs`, and **Unity only binds a MonoBehaviour to
a script asset when the class name matches the file name.** Core 60/60, EditMode 105/105,
PlayMode 55/55, Headless, Arena, Audio and MapGeometry were ALL green on that commit, because
every one of them runs in the editor and the editor resolves the stub by class name.

⚠️ **It reads the scene as text and never opens it.** Opening the scene is what makes the fault
invisible, and saving it afterwards would rewrite the stub away without anyone learning it had
been there.

⚠️ **`GameBuilder` runs it before every build** and refuses to write a player that would carry
the fault.

```bash
Unity -batchmode -nographics -projectPath . -executeMethod TumbangPreso.EditorTools.UgsCheck.Run -logFile -
```

`Logs/ugs-check.txt`. **The only check here that asks a server rather than a file.** It signs in
anonymously, allocates a Relay server, creates and deletes a Lobby, and names the dashboard
toggle behind each failure. Run it whenever online play misbehaves, because the five setup steps
it covers live in the Unity account and leave no trace in this repository at all: one of them
was skipped for a week without anything going red. `docs/Port_Plan.md` §5 has the table.

⚠️ **It takes no `-quit`**, unlike the four above. The service calls are async, so the check pumps
`EditorApplication.update` until they return and exits the editor itself.

⚠️ **Batchmode cannot see whether you are signed in**, because the access token is handed to the
editor by the Hub. It reports that as unknown rather than as a failure. Step 3 passing proves
the sign-in anyway.

All five are also on the **Tumbang Preso** menu in the editor.

---

## 4 · Actually playing it

```bash
"/c/Program Files/Unity/Hub/Editor/6000.5.8f1/Editor/Unity.exe" -projectPath .
```

Open **`Assets/TumbangPreso/Scenes/VerticalSlice.unity`** and press Play.

⚠️ **The scene is generated, not authored.** If you break it, regenerate it from the
**Tumbang Preso → Build Vertical Slice Scene** menu. Do not hand-fix it, because the next
regeneration overwrites your fix. If the arena is wrong, the constant is wrong.

**What you should see:** a grey plane with a white chalk square, a squat cylinder on the mark,
four capsules, and a HUD with the round timer, four scores, a stamina bar and a status stack.
Four bots play a full six-minute match and the taya rotates every round.

### To drive a player yourself

Every seat is a bot, because a headless probe has no keyboard. To take one:

1. Select **Seat0** in the hierarchy.
2. **Remove** the `AIController` component. That is the whole switch: a bot and a human write
   the same intent table, so with the AI gone the seat is simply waiting for input.
3. **Add** `PlayerInputReader` and drag `Assets/TumbangPreso/Input/TumbangPreso.inputactions`
   into its Actions field.

Controls are the Godot ones: **WASD**, **Shift** sprint, **Space** jump, **left-click** throw
(or punch as the taya), **E** contextual grab / shove / lata reset, **E held** or **right-click**
lunge, **B** emote wheel.

---

## What is worth your judgement, and what is not

**Do not spend your time checking numbers.** 58 automated tests already cover the rules, the
scoring, the bounds, reconnection and the match loop, and they are far better at it than a human
playthrough.

**Spend it on the one thing no test can answer: does the movement feel like the Godot build?**
That is Phase 3's real exit criterion and the single largest risk in the whole port, because
Godot's `move_and_slide` and Unity's `CharacterController` do not resolve collisions the same
way. Run both builds side by side and compare:

- walking and sprinting acceleration, and how sharply a direction change lands
- how a body slides along the chalk boundary when the taya is clamped
- whether the throw arc leaves the hand where you expect
- what standing on another player's head does

Anything that feels off there is a real finding and I cannot get it from a probe. Everything
else, the tests will have caught first.
