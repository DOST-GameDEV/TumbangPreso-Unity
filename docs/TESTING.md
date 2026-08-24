# How to test this

Three ways in, cheapest first. **Do the cheap ones before opening Unity**: they run in seconds
and they tell you whether anything is broken before you spend two minutes on an editor launch.

---

## 1 · The rules, in one second, no Unity at all

```bash
dotnet test Core.Tests/TumbangPreso.Core.Tests.csproj
```

**Expect:** `Passed! - Failed: 0, Passed: 32`

This is the balance layer: match rotation, scoring, traits, stamina, throw legality, the hit
window, the combat geometry. Every number here was reproduced from the measurements recorded in
the Godot `Design.md`, so **if this goes red, the tuning changed**, and that is the one thing in
this port that must never change by accident.

---

## 2 · Everything else, headless, no clicking

Two suites. Both launch Unity themselves and print a pass count.

```bash
"/c/Program Files/Unity/Hub/Editor/6000.5.8f1/Editor/Unity.exe" -batchmode -runTests -nographics -projectPath . -testPlatform EditMode -testResults Logs/edit.xml -logFile Logs/edit.log
```

**Expect 21 passed.** Wiring, the arena bounds, reconnection, seat reclaim, leader election,
join codes, names, emotes.

```bash
"/c/Program Files/Unity/Hub/Editor/6000.5.8f1/Editor/Unity.exe" -batchmode -runTests -nographics -projectPath . -testPlatform PlayMode -testResults Logs/play.xml -logFile Logs/play.log
```

**Expect 5 passed.** This one actually **runs the game**: a full four-round match completes with
the taya rotating through every seat, bodies stay on the ground, the taya is held in the box, an
attacker walks freely through the chalk, and sprinting drains to fatigue in the time the
arithmetic says it should.

Read the counts out of the XML:

```bash
powershell -c "$x=[xml](gc Logs/play.xml); $x.'test-run'.passed + '/' + $x.'test-run'.total"
```

⚠️ **If a run produces no log at all, Unity never started.** It leaves child processes holding
the project lock after it exits. Kill `Unity`, `Unity.ILPP.Runner` and `UnityPackageManager`,
then try again. An empty log is not a passing run.

---

## 3 · The four checkers

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

Both are also on the **Tumbang Preso** menu in the editor.

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
