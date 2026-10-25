# Unity project context

Analyzed 2026-10-25 at BugFix `47487eda7` before documentation integration.
Configuration and source confirm the facts below; validation results live in
dated reports. Read [AGENTS](../AGENTS.md) and [TODO](TODO.md) for current scope.

## Environment and architecture

| Area | Confirmed source |
|---|---|
| Editor | Unity 6000.5.8f1, `ProjectSettings/ProjectVersion.txt` |
| Rendering | URP 17.5, GraphicsSettings and manifest |
| Input/UI | Input System 1.20, uGUI; programmatic screen/theme helpers |
| Network | NGO 2.13, embedded Unity Transport; UGS authentication, Lobby/Relay |
| Rules | Embedded `com.tumbangpreso.core`; no engine references; .NET compiles same files |
| Tests | Core.Tests; Unity EditMode, PlayMode and TestSupport assemblies |
| Platforms | Existing Windows, macOS, Android and WebGL builders; installed modules vary |

MonoBehaviours own runtime/visual lifecycle. Core owns rules and tuning;
MatchDirector/MatchHost/RoundDirector own match flow. Host-authoritative actions
flow through NetAuthority and MatchRpc; owners predict only established paths.
CharacterMotor is movement; HeroAbilitySystem and per-hero kits own powers.
Separate camera/viewmodel/presentation code observes gameplay state.

## Code map

- `Packages/com.tumbangpreso.core/Runtime`: source shared with `Core/*.csproj`.
- `Assets/TumbangPreso/Runtime`: runtime assembly, including Abilities, Camera,
  Input, Net, UI, Map, Visual, settings and replay owners.
- `Assets/TumbangPreso/Editor`: editor-only assembly, builders and native probes.
- `Assets/TumbangPreso/Tests`: Tests/EditMode, PlayTests and shared TestSupport.
- `ArtSource`, `MapSource`, `tools`: authored source, provenance and regeneration.

Namespaces follow `TumbangPreso` and feature subnamespaces. Preserve serialized
names/GUIDs, public seams, current brace/field style and event cleanup patterns.
Inspect neighboring code rather than applying a cast-wide or package-wide refactor.

## Startup and scenes

Build Settings starts at `Scenes/Ui/SplashScreen.unity`; SceneFlow and boot/menu
components drive login/loading/Home into match setup. Menu names are not a promise
that each old serialized screen is the current visual route.
Enabled map scenes include Eskinita, IlalimNgTulay, LagoonCove, Kanto, Arena and
Payo. BayanPlaza and SaBubong are disabled historical maps. Match selection uses
the current source catalog; do not reactivate retired maps to satisfy old probes.

## Tools and qualification

Python/.NET and the installed matching Unity Editor support validation.
No connected Unity MCP was available during this audit; the manifest's pipeline
package alone does not establish a connected tool. Use [TESTING](TESTING.md) and
[setup](WORKSTATION_SETUP.md), fresh XML and actual graphics/peers where required.
Current-source LAN/Relay/reconnect and spectator acceptance are active work;
older passing reports do not certify the present build.

## Constraints

Never duplicate Core, reorder roster IDs, confer authority on replicas, mutate
user profiles or discard concurrent edits. Read relevant [working rules](WORKING_RULES.md).
Paete/Phaister authored work stays protected. `EconomyRules.UnlockedHeroes` owns
the six current playable heroes, including Rago; nine hero entries exist.
