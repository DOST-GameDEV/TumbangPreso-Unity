# Tumbang Preso

A Unity 6 game about the Filipino street game: one taya guards a tin can while
three attackers throw their slippers and run back to retrieve them. Four players,
normally eight rounds. Classic keeps the street-game rules; Hero Strike adds kits.

## Start here

- [AGENTS](AGENTS.md): short working rules.
- [Project context](docs/UnityProjectContext.md): architecture, source map and setup.
- [Current work](docs/TODO.md) and [checkpoint](docs/ACTIVE_REWORK_LEDGER.md): priorities and resume state.
- [Documentation routes](docs/README.md): read only the relevant topic.

## Current source

Unity version comes from `ProjectSettings/ProjectVersion.txt`. The project uses
URP, the Input System, uGUI, Netcode for GameObjects and UGS Lobby/Relay.

`Roster.cs` defines the append-only character, can and slipper catalogs.
There are nine hero entries; the current playable gate is
`EconomyRules.UnlockedHeroes`: Dante, Rago, Nemu, Phaister, Amihan and Paete.
The other hero entries remain visible but locked. Stable IDs retain their original
names for saves, replay and network compatibility. Never reorder the catalogs.

| Path | Responsibility |
|---|---|
| `Packages/com.tumbangpreso.core/Runtime/` | Engine-free rules, scoring, geometry and tuning |
| `Core/`, `Core.Tests/` | .NET project compiling the same rules, plus tests |
| `Assets/TumbangPreso/Runtime/` | Gameplay, input, camera, UI and networking |
| `Assets/TumbangPreso/Editor/`, `Tests/` | Authoring tools and Unity checks |
| `ArtSource/`, `MapSource/` | Authored sources and provenance |
| `tools/` | Authoring, source audits, build and peer-validation tools |
| `docs/` | Live guides; dated reports and archives are loaded on demand |

## Test and build

```powershell
dotnet test Core.Tests/TumbangPreso.Core.Tests.csproj --nologo
python tools/audit_docs.py
```

Use [TESTING](docs/TESTING.md) for isolated Unity tests and fresh XML checks.
PlayMode and visual tests require graphics. Compilation, local tests and real
multiplayer acceptance are separate evidence. [Setup](docs/WORKSTATION_SETUP.md)
describes Editor discovery and profiles. Player builds go only to the canonical
checkout's `Builds/<platform>/` with an explicit output argument.

The [Godot original](https://github.com/DOST-GameDEV/DOST-GameDev) is historical
reference. This Unity repository owns the current game.
