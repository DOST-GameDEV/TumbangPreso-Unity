# Tumbang Preso, Unity build

The Unity 6 build of **Tumbang Preso**, 1st place at the Gear Up NCR Esports Game Dev
Challenge and NCR's entry at the nationals in General Santos City.

Four players. Four rounds. One *taya*. A defender guards the lata inside a chalk box while
three attackers throw tsinelas at it, then have to run in and get them back. **The tension is
the retrieval, not the throw.**

## Two modes

| | **CLASSIC** | **HERO STRIKE** |
|---|---|---|
| | The street game. Twelve characters, no powers. | The competitive mode. Five heroes, two skills and an ultimate each. |
| For | Players who want less happening on screen | A higher skill ceiling, aimed at bracket play |

Both ship and neither is the "real" one. The reasoning, and everything that follows from it,
is in **[`docs/VISION.md`](docs/VISION.md)**.

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
Assets/TumbangPreso/Tests/        EditMode tests, and PlayMode tests and probes
docs/                             plan, ledger, design, vision, open work
tools/                            screenshot and capture scripts for the built player
```

## Build and test

```bash
dotnet test Core.Tests/TumbangPreso.Core.Tests.csproj
```

```bash
"/c/Program Files/Unity/Hub/Editor/6000.5.8f1/Editor/Unity.exe" -batchmode -runTests -nographics -projectPath . -testPlatform EditMode -testResults Logs/tests.xml -logFile Logs/tests.log
```

```bash
"/c/Program Files/Unity/Hub/Editor/6000.5.8f1/Editor/Unity.exe" -batchmode -runTests -projectPath . -testPlatform PlayMode -testResults Logs/play.xml -logFile Logs/play.log
```

⚠️ **PlayMode must NOT be given `-nographics`; it crashes the editor and still exits 0.**
Always read the result `.xml`, never the exit code. `CLAUDE.md` § 6 has the rest of the traps
on this machine.

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
anything, including a design document, this repository is the current one. `CLAUDE.md` § 2.
