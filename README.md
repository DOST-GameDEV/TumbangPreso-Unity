# Tumbang Preso, Unity build

The Unity 6 build of **Tumbang Preso**, 1st place at the Gear Up NCR Esports Game Dev
Challenge and NCR's entry at the nationals in General Santos City.

Four players. Four rounds. One *taya*. A defender guards the lata inside a chalk box while
three attackers throw tsinelas at it, then have to run in and get them back. **The tension is
the retrieval, not the throw.**

## Two modes

| | **CLASSIC** | **HERO STRIKE** |
|---|---|---|
| | The street game. Twelve characters, no powers. | The competitive mode. Six heroes, two skills and an ultimate each. |
| For | Players who want less happening on screen | A higher skill ceiling, aimed at bracket play |

Both ship and neither is the "real" one. The reasoning, and everything that follows from it,
is in **[`docs/VISION.md`](docs/VISION.md)**.

## What you pick

Three tabs, and the three meters are the same three keys behind every one of them: a person's
SPEED is a lata's RESET is a tsinelas's FLIGHT.

| | |
|---|---|
| **12 people** in Classic, **6** in Hero Strike | Two separate casts, not a subset |
| **6 lata** | Each identifiable by silhouette alone, because a label dies at arena distance |
| **10 tsinelas** | Entry 0 stays neutral: it is what an unpicked slipper resolves to |

⚠️ **These three lists are APPEND-ONLY.** The index of an entry crosses the wire as a bare
int, so inserting or deleting a row silently repoints every pick above it on any peer or save
file holding the old number. `Roster.cs` carries the full reasoning.

## Start here

| Read | For |
|---|---|
| [`CLAUDE.md`](CLAUDE.md) | The rules of this repository. Read first, always. |
| [`docs/VISION.md`](docs/VISION.md) | What the game is FOR. The two modes, the readability budget, how a player learns a power. |
| [`docs/TODO.md`](docs/TODO.md) | What is actually open right now. |
| [`docs/README.md`](docs/README.md) | Index of every other document. |

## Where things stand

The game runs end to end in both modes: menus, character select, four-round matches with
bots, the hero ability layer, spectator, reconnect and the netcode layer.
[`docs/Port_Ledger.md`](docs/Port_Ledger.md) is the file-by-file status and the definition of
done; [`docs/TODO.md`](docs/TODO.md) is what is left.

## Repository layout

```
Packages/com.tumbangpreso.core/   the rules, as plain C# with no UnityEngine reference
Core/                             the same files, compiled by dotnet for `dotnet test`
Core.Tests/                       the balance assertions that prove the tuning survived
Assets/TumbangPreso/Runtime/      the game
Assets/TumbangPreso/Editor/       builders, probes and the five checks
Assets/TumbangPreso/Shaders/      the toon pass, the colour grade, the world outline
Assets/TumbangPreso/Tests/        EditMode tests, and PlayMode tests and probes
docs/                             plan, ledger, design, vision, open work
tools/                            asset generators, source audits, and player capture scripts
```

## Build and test

```bash
dotnet test Core.Tests/TumbangPreso.Core.Tests.csproj
```

```bash
"/c/Program Files/Unity/Hub/Editor/6000.5.8f1/Editor/Unity.exe" -batchmode -runTests -nographics -projectPath . -testPlatform EditMode -testResults Logs/tests.xml -logFile Logs/tests.log
```

```bash
"/c/Program Files/Unity/Hub/Editor/6000.5.8f1/Editor/Unity.exe" -batchmode -runTests -projectPath . -testPlatform PlayMode -testCategory "!WallClock" -testResults Logs/play.xml -logFile Logs/play.log
```

All five editor checks in one launch:

```bash
"/c/Program Files/Unity/Hub/Editor/6000.5.8f1/Editor/Unity.exe" -batchmode -projectPath . -executeMethod TumbangPreso.EditorTools.Checks.RunAll -logFile Logs/checks.log
```

⚠️ **PlayMode must NOT be given `-nographics`; it crashes the editor and still exits 0.**

⚠️ **`-testCategory "!WallClock"` is part of that command, not an optimisation.** One probe
runs a round at 1x for about 80 real seconds, so its result depends on how busy the machine
is; it has failed and passed on consecutive runs with nothing changed.

⚠️ **Always read the result `.xml`, never the exit code.** A crash and a genuine failure both
come back as 0. `CLAUDE.md` § 7 has the rest of the traps on this machine, including the stale
`Temp/UnityLockfile` that looks exactly like a broken install.

## The one rule that matters

**`Packages/com.tumbangpreso.core/` must never acquire a `UnityEngine` reference.** It holds
the match rules, the scoring, the trait tables, the stamina model, throw legality and the
combat geometry: every number in the game that was arrived at by measurement rather than by
taste. Keeping it engine-free is what lets those numbers be asserted in a second instead of
playtested for an afternoon, and it is the entire verification strategy. The asmdef enforces
it with `noEngineReferences`. Do not add an exception.

## The original

The Godot 4.7 original is [DOST-GameDev](https://github.com/DOST-GameDEV/DOST-GameDev). It is
**frozen reference for the old version and is not edited.** Where the two disagree about
anything, including a design document, this repository is the current one. `CLAUDE.md` § 1.
