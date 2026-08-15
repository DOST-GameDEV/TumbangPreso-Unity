# Port Ledger — every Godot source, and where it went

This file exists because the port kept "finishing" while whole features were
missing. It is the authoritative checklist. **Nothing is done until every row
below reads CONVERTED.**

Scope measured 2026-08-15 against `DOST-GameDev` @ Godot 4.7:

- **45 gameplay scripts, 31,314 lines** of GDScript under `scripts/`
- **27 scenes** under `scenes/`
- **14 input actions**, **9 autoload singletons**
- (`tools/` is ~20k more lines of dev probes. NOT game features, NOT in scope,
  except as a reference for how a system is supposed to behave.)

Unity side today: ~13k lines of C#, of which ~1,900 are editor converters.

## ⚠️ Unity rules the conversion has to obey, each found by a shipped failure

These are not style. Each one produced a build that looked correct in the editor
and was broken for a player, which is the most expensive kind of bug this port
can produce.

1. **One MonoBehaviour per file, named after the class.** Unity only makes a
   MonoScript asset for the class matching the FILE name. A second MonoBehaviour
   in the same file serialises into a scene as an embedded `--- !u!115` stub, and
   the player then reports `The file 'levelN' is corrupted!` and dies on load.
   Every converted screen carried four to eleven of those. `TscnUiImporter`
   checks this now and fails the import rather than writing such a scene.
2. **A scene may only reference assets on disk.** `Sprite.Create` at import time
   produces an object the scene does not own; same crash. Style boxes and scrim
   gradients are baked to PNG (`StyleBoxBaker`, `BakeGradient`).
3. **`AddComponent` cannot carry an inspector reference.** Anything a
   code-installed component needs must be loaded by the component itself. This is
   what left `PlayerInputReader` with no action asset in every build, so the
   human seat could not move.
4. **A renderer created in code has no material** and draws as a magenta error
   blob, and any MaterialPropertyBlock written at it is discarded.
5. **A property block writes a NAMED property**, silently doing nothing when the
   shader has none. `EnvColourPass` reported 418 of 434 renderers repainted while
   changing nothing on screen. Tint through a material variant and check
   `HasProperty`.
6. **An asset nothing references is stripped from the build.** The 32 animation
   clips per character live inside the `.glb`; without a serialised reference the
   whole cast stood still in the player only.

## How to read the status column

- **CONVERTED** — ported function-by-function against the .gd, behaviour verified
- **PARTIAL** — a file exists and compiles, but does not do everything the .gd does
- **MISSING** — no counterpart exists at all

Line counts are given for both sides. A large gap on a PARTIAL row is the honest
size of the remaining work. Ratios are a smell test, not a spec: `character_roster.gd`
is CONVERTED at half the lines because GDScript dictionaries became typed records.

## ⚠️ The camera directive — read before touching any camera

A previous session recorded "the game is first person, TPP was a mistake." **That
is wrong and must not be acted on.** `camera_rig.gd` has FOUR third-person cases:

1. **Prop is always TPP.** `camera_rig.gd:5` — "Person is ALWAYS first-person,
   Prop (Can/Slipper) is ALWAYS third-person." The mode is derived from
   `_character.is_person` and is asserted; nothing else may write `_mode`.
2. **Emote view** (`camera_rig.gd:425`). A Person swings to TPP for the duration
   of an emote and returns to FPP. The emote camera ORBITS, it does not steer —
   mouse moves the camera around the body, never the body itself. Pitch clamps
   to -35/+20, separate from the gameplay clamp. **Local only**: the emote is
   replicated, the camera swing is not, or every peer would spin when one
   player danced.
3. **Carried-prop follow** (`_update_tpp_carry_follow`) — ⚠️ **VESTIGIAL, DO NOT
   PORT AS LIVE BEHAVIOUR.** An earlier note here (mine, 2026-08-15) described it
   as a working fourth case. It is not: `camera_rig.gd:750` declares
   `var carrier: CharacterBase = null` and nothing ever assigns it, so every
   branch below that line is unreachable in the shipped build. It dates from when
   props were playable units; §12 deleted playable props and left the function
   behind. The constants (`TPP_CARRY_MOUNT_HEIGHT = 0.6`, the -15° base tilt) are
   real but nothing reads them. Port it only if playable props ever come back.
4. **Spectator** — separate rig, see `spectator_camera.gd` below.

The real earlier mistake was narrower: an *overhead follow* camera was built that
matched none of these. Fix that framing, do not delete TPP.

## Autoload singletons (9)

Godot autoloads are always-on globals. Unity has no equivalent; these become
`GameServices` entries or `RuntimeInitializeOnLoad` singletons. All 9 must exist.

| Godot autoload | Lines | Unity | Status |
|---|---|---|---|
| `audio_manager.gd` | 1125 | `AudioDirector` + `AudioCues` + `MusicDirector` (382) | PARTIAL |
| `round_manager.gd` | 476 | `RoundDirector.cs` (219) | PARTIAL |
| `match_manager.gd` | 217 | `MatchDirector.cs` (97) | PARTIAL |
| `network_manager.gd` | 1413 | `NetSession` + `LobbySession` + `MatchRpc` (800) | PARTIAL — verbs, emotes, ready gate, picks, late join; host migration pending |
| `lan_beacon.gd` | 323 | `LanBeacon.cs` (238) | PARTIAL |
| `server_query.gd` | 536 | `ServerQuery.cs` (215) | PARTIAL — browse and expiry; join codes pending |
| `game_launch.gd` | 301 | `GameLaunch.cs` (108) | CONVERTED — map registry, pending action, seating |
| `settings_manager.gd` | 703 | `GameSettings` + `Rebinding` (400) | CONVERTED — sliders, rebinding, applied on load |
| `debug_player_switcher.gd` | 420 | `DebugPlayerSwitcher.cs` (115) | PARTIAL — seat drive, cycle, readout |

## Characters and objects

| Godot | Lines | Unity | Status |
|---|---|---|---|
| `character_base.gd` | 1981 | `CharacterMotor` + `CombatVerbs` + `StatusStack` (651) | PARTIAL |
| `character_visual.gd` | 2182 | `CharacterVisual` + `CharacterAnimator` + `ImpactBurst` (560) | PARTIAL — clips, flash, burst wired; prop attachments and remote smoothing pending |
| `carrier.gd` | 536 | `Carrier.cs` (210) | PARTIAL |
| `character_nameplate.gd` | 165 | `CharacterNameplate.cs` (155) | CONVERTED — ring, tag, role colour, distance fade |
| `slipper.gd` | 1630 | `Slipper.cs` (280) | PARTIAL — flight, bounce, spin, void recovery; hand attach and net sync pending |
| `lata.gd` | 534 | `Lata.cs` (175) | PARTIAL — topple, roll, hit window; skins and net sync pending |

## Systems

| Godot | Lines | Unity | Status |
|---|---|---|---|
| `main.gd` | 3595 | `MatchHost.cs` + `MatchInstaller` (370) | PARTIAL — local half; netcode half pending |
| `ai_controller.gd` | 2225 | `AIController` + `AiTuning` + `AiPersonalityRoll` (800) | PARTIAL — tiers, personalities, 13-plan machine; lane sampling and unstick pending |
| `camera_rig.gd` | 1111 | `CameraRig` + `ViewmodelArms` (640) | CONVERTED — FPP, prop TPP, emote swing, arms; carry-follow is dead code upstream |
| `spectator_camera.gd` | 431 | `SpectatorCamera.cs` (431) | CONVERTED — call sites pending, see below |
| `character_roster.gd` | 757 | `Roster` + `RosterBook` (411) | CONVERTED (20/20 validated) |
| `env_toon_pass.gd` | 391 | `EnvColourPass.cs` (185) | CONVERTED — tints, foliage, laundry sway |
| `trajectory_preview.gd` | 273 | `TrajectoryPreview.cs` (113) | PARTIAL |
| `hazard_zone.gd` | 133 | `HazardZone.cs` (108) | CONVERTED — slow zone, visual disc, round-scoped lifetime |
| `game_version.gd` | 56 | `GameVersion.cs` (80) | CONVERTED — reads `Application.version`, now 4.68 |
| `kill_plane.gd` | 26 | `KillPlane.cs` (62) | PARTIAL — logic and spawn anchor done, height still borrowed |

### `spectator_camera.gd` — CONVERTED 2026-08-15, audited line by line

All 19 behaviours of the .gd are present in `SpectatorCamera.cs`, checked against the
source after writing rather than from memory: the three speed constants and their
two human-instruction retunes, the ±88 pitch limit, position smoothing at 14.0 with
rotation deliberately unsmoothed, follow distance and its 0.34 lift ratio, both POV
eye heights and the 0.34 forward offset, FOV 78 / far 400, the wheel meaning two
different things in two modes, Tab / F / V, the every-frame view re-claim, the
free-flight vector with jump and `SpectatorDown`, the follow-list rebuild with its
fallback scan, `ControlsText()`, `StatusText()`, and the legend's role suffix.

Three things changed on purpose, each commented in the file:

- **Start position Z is negated** — `(0, 9, 14)` in Godot is `(0, 9, -14)` here. Same
  handedness flip the map conversion uses. Left alone, the mode opens looking at an
  empty street with the match behind the camera.
- **Pitch signs are negated** — Godot's `rotation.x` is positive looking up, Unity's
  euler X is positive looking down. The .gd's `-26` start is `+26` here.
- **Yaw adds instead of subtracts** — same flip; copying the sign would invert
  mouse-look for spectators only.

Held invariants: it is a plain Transform with a Camera and no collider, so clipping
is structural; it reads hardware directly and never an `InputIntent`, which is what
keeps a bot from flying it now that the AI writes intents exclusively.

**Still pending — its call sites, all of which live in the unported `main.gd`:**
seat -1 / no character spawned, exclusion from the ready gate, the placeholder-AI
fill for the vacated slot, the HUD's spectator branch polling `StatusText()`, and
the on-screen legend. The camera itself is done; nothing selects it yet.

Registration into the followable set was moved onto `CharacterMotor.OnEnable`
rather than waiting for `main.gd`, so Tab works as soon as units exist.

### `spectator_camera.gd` — the control set, for reference

Free-fly camera with three modes: free, follow, POV. Every constant here is from
the .gd, transcribe them rather than re-tuning:

- `BASE_SPEED 3.6`, boost ×`2.5` on `sprint`, speed steps ×`1.35` clamped `1.2`–`40.0`
- Pitch limit `88°`, move smoothing rate `14.0`
- Follow distance `6.5`, clamped `1.2`–`30.0`, lift ratio `0.34`
- POV eye height `1.45` Person / `0.42` Prop, forward offset `0.34`
- `Tab` cycles the follow target, `V` toggles POV — both read in `_input`, NOT
  `_unhandled_input`, because the HUD is a live CanvasLayer that eats Tab first
  (`spectator_camera.gd:233` explains this at length — read it before rewiring)
- Vertical movement uses `jump` up / `spectator_down` down. **Not** `guard_dash`,
  which no longer exists and threw every frame until it was fixed.
- `status_text()` drives the spectator's on-screen readout

## UI (scripts/ui — 21 files)

`.tscn` LAYOUTS for 11 screens are converted (see `TscnUiImporter`). Behaviour is
a separate job, tracked here. A converted layout with no script bound is PARTIAL.

| Godot | Lines | Unity | Status |
|---|---|---|---|
| `match_setup.gd` | 2015 | `ConvertedMatchSetup.cs` (440) | PARTIAL — rows, seats, spectate, live map preview; netcode lobby half pending |
| `hud.gd` | 1587 | `Hud.cs` (420) | PARTIAL — cards, clock, scores, stamina on the theme's Hud* set; per-row scoreboard and toasts pending |
| `multiplayer_setup.gd` | 1015 | `LobbySession.cs` (287) | PARTIAL |
| `character_preview.gd` | 623 | `ModelPreview.cs` (215) | PARTIAL — render, framing, turn, drag; idle clip pending |
| `ui_theme.gd` | 551 | `UiTheme` + `GodotTheme` + `StyleBoxBaker` (520) | CONVERTED — variations, StyleBox geometry and the baked nine-slices |
| `tutorial.gd` | 462 | `TutorialContent` + `ConvertedTutorialPanel` (230) | PARTIAL — all 8 pages on the converted scene; 3D props pending |
| `you_card.gd` | 430 | `YouCard.cs` (185) | PARTIAL — role, name, live meters; ready flash pending |
| `settings_panel.gd` | 429 | `SettingsPanel` + `Rebinding.cs` (330) | CONVERTED — rebinding, conflicts, reset |
| `emote_wheel.gd` | 422 | `EmoteWheel.cs` (215) + `Emotes.cs` | CONVERTED — hold, steer, release |
| `character_select.gd` | 341 | `CharacterSelectScreen` (200) | CONVERTED — tabs, chalk pips, live 3D |
| `match_result.gd` | 339 | `MatchResult.cs` (215) | PARTIAL — board and single-player rematch; peer vote pending netcode |
| `credits_panel.gd` | 292 | `CreditsContent.cs` + `CreditsPanel` (250) | CONVERTED — CC-BY strings verbatim |
| `role_swap_card.gd` | 274 | `RoleSwapCard.cs` (250) | CONVERTED — intermission timeline, swap, standings |
| `arrow_button.gd` | 262 | `ArrowButtonView.cs` (250) | CONVERTED — unfurl, hover, press, both cues |
| `offscreen_indicators.gd` | 211 | `OffscreenIndicators.cs` (175) | CONVERTED — edge arrows, wired into the HUD |
| `map_preview.gd` | 165 | `MapPreview.cs` (150) | PARTIAL — sway, cache, silence; needs map prefabs |
| `splash_screen.gd` | 107 | `SplashScreen.cs` (154) | CONVERTED |
| `mode_select.gd` | 96 | `ConvertedModeSelect.cs` | CONVERTED |
| `main_menu.gd` | 85 | `ConvertedMainMenu.cs` | CONVERTED — in-place overlays, pennants re-unfurl |
| `debug_bar.gd` | 47 | `DebugBar.cs` (110) | CONVERTED — deliberately unstyled |
| `pause_layer.gd` | 21 | `PauseWatcher` in `MatchInstaller.cs` | CONVERTED — see note |

## Scenes (27) — one row each, so an audit can check them

⚠️ Named individually on purpose. This section used to say "both maps and 11 UI
screens", which no script can verify and no reader can check against the source
tree. A collective count is how a missing scene hides.

| Godot scene | Status |
|---|---|
| `Eskinita.tscn` | CONVERTED — 416 objects, 0 missing, walls at ±8.60, light + fog + sky + colour pass |
| `BayanPlaza.tscn` | CONVERTED — 553 objects, 0 missing, light + fog + sky + colour pass |
| `SplashScreen.tscn` | CONVERTED |
| `MainMenu.tscn` | CONVERTED — real backdrop, logo, arrow buttons |
| `ModeSelect.tscn` | CONVERTED |
| `MatchSetup.tscn` | CONVERTED — rows, seats, spectate, live map behind |
| `MultiplayerSetup.tscn` | PARTIAL |
| `CharacterSelect.tscn` | CONVERTED — tabs, chalk pips, live 3D |
| `MatchResult.tscn` | CONVERTED — board rebuilt in code |
| `SettingsPanel.tscn` | CONVERTED — incl. rebinding |
| `CreditsPanel.tscn` | CONVERTED — CC-BY strings verbatim |
| `HUD.tscn` | PARTIAL — 35 nodes converted; the live HUD is still built in code, on the same theme |
| `ArrowButton.tscn` | CONVERTED — inlined per instance, `ArrowButtonView` drives it |
| `Tutorial.tscn` | CONVERTED — 8 pages; 3D props pending |
| `YouCard.tscn` | CONVERTED |
| `RoleSwapCard.tscn` | CONVERTED |
| `OffscreenIndicators.tscn` | CONVERTED |
| `DebugBar.tscn` | CONVERTED |
| `ViewmodelArms.tscn` | CONVERTED — baked transforms carried across |
| `CameraRig.tscn` | **MISSING** — ⚠️ baked transforms; read `camera_rig.gd:21` first |
| `CharacterBase.tscn` | **MISSING** — built in code by `MatchInstaller` instead |
| `CanVisual.tscn` | **MISSING** |
| `TsinelasVisual.tscn` | **MISSING** |
| `Lata.tscn` | **MISSING** — built in code |
| `Slipper.tscn` | **MISSING** — built in code |
| `Main.tscn` | **MISSING** — holds the kill plane's real transform |
| `PremiseIcon.tscn` | **MISSING** — the tutorial's 3D props |

### `pause_layer.gd` — the one file the port does not need

Its entire 21 lines exist to solve a Godot-specific problem: a node at the default
`PROCESS_MODE_INHERIT` stops receiving `_unhandled_input` once the tree is paused,
including the very Esc press meant to resume. Godot's fix was a dedicated
`PROCESS_MODE_ALWAYS` CanvasLayer whose only job is to survive the pause it causes.

Unity's `Update` is frame-driven and keeps running at `timeScale = 0`, so
`PauseWatcher` reads Escape while paused without any equivalent trick. Marked
CONVERTED rather than dropped, because "we do not need this" is a claim that has to
be written down and checked, not assumed.

## Bot difficulty — the tiers landed 2026-08-15, the plan machine has not

`AiTuning.cs` in the engine-free core holds all three tiers (Bata / Normal / Astig)
with every one of their 17 tuning values, plus the 36 tier-independent geometry and
cadence constants. 7 tests assert them against `ai_controller.gd`.

**Three real divergences were found and fixed, not cosmetic ones:**

- `ArriveSlop` was **0.35**; the .gd has **0.55**. The tighter value makes bots jitter
  on arrival rather than settle on a mark.
- Lunging was gated on `tier != Easy`, so **Bata and Astig lunged identically**. It is
  now range (1.9 / 2.6 / 3.1) AND cone — a half-angle where smaller is stricter, so
  Astig's 28° is the disciplined one and Bata's 55° the wild one.
- Sprinting was gated on `tier == Hard`, so **Normal never sprinted at all**. It is now
  distance past 5.0 m and a stamina reserve the tier holds back (Bata spends
  everything, Astig keeps 0.45 for a chase that matters).

⚠️ **The saved difficulty was being ignored entirely.** The settings panel wrote
`AiDifficulty` and nothing ever read it back, so every bot in every match played at
Normal regardless of what the player picked. `MatchInstaller` now applies it.

**Still missing:** the plan state machine itself — lane sampling, intercept
prediction, stalk patience, stuck detection and unsticking, slipper claim TTL, the
loiter walk, and the deliberate mistake roll. That is the bulk of the 2,225 lines.
The numbers are now in place for it to be written against.

## Ready-up phase — CONVERTED 2026-08-15 (local half)

`ReadyGate.cs`, from the ready-phase half of `main.gd` (~lines 1036-1195). Free-roam
window, "Press [R] when you're ready", the ready gesture other players can see, then
3 · 2 · 1 · GO! at 1.0 s a tick and 0.5 s on GO. The round begins when the countdown
finishes, never on the press, and `_countingDown` stops a second press restarting it.

`SliceRunner.AutoStart` is now off whenever the gate is used, or the round would begin
underneath the countdown. Headless probes set `MatchInstaller.UseReadyGate = false`,
because nobody is there to press R.

⚠️ **The networked half is NOT ported.** Godot's host counts one press per connected
human PEER — never per character, because a 2v2 always has four characters and an AI
cannot press R, so counting characters leaves a solo host waiting forever for three
bots to agree. Spectators are excluded for the same reason. That needs
`NetworkManager.playing_peer_count()`, which is unported. **Do not approximate it by
counting characters.**

## Input actions (14) — all must exist in the Input System asset

`move_left` `move_right` `move_up` `move_down` `jump` `sprint` `grab` `lunge`
`special_ability` `emote_wheel` `ready_up` `spectator_down` `clean_feed`
`toggle_fullscreen`

`ready_up` is the missing ready-up phase. `spectator_down` is spectator descent.
`clean_feed` hides HUD for capture. None of these may be dropped — each is
rebindable through `settings_panel.gd`, and `tools/input_probe.gd` checks them
for conflicts.

## Constant audit — run it again after every balance change

Every `const` in `character_base.gd`, `slipper.gd`, `lata.gd` and `round_manager.gd`
was extracted and compared against every `const` in the Unity runtime on 2026-08-15,
by name (snake → Pascal) and by value.

**Result: 77 constants on each side, and ZERO value mismatches.** The balance layer
is faithful. That is the single most reassuring measurement taken in this port so far,
because it is the layer that cannot be verified by looking at a screenshot.

Twelve Godot constants had no Unity counterpart. All twelve are now in `Balance.cs`
with their original reasoning: `BounceRestitution`, `MinPowerScale`,
`SlipperRestHeight`, `SlipperSpinSpeedDeg`, `SlipperTumbleSpeedDeg`,
`SlipperModelLength`, `VoidY`, `OwnerRimStrength`, `HitstopDuration`,
`HitstopTimeScale`, `LandSfxMinSpeed`, `SlipperSyncInterval`.

⚠️ **The numbers landing is not the feature landing.** Nothing reads most of them yet
— slipper flight, hitstop and the owner rim glow are all still PARTIAL rows above.
They are transcribed first so the port cannot quietly re-derive a number by taste.

⚠️ Two earlier passes of this audit gave WRONG answers and both are worth knowing.
Matching by value alone reports `BounceRestitution` as present because `LungeActiveTime`
happens to also be 0.45. Scanning only the Core package reports `PerchNormalMin` as
missing when it is a private const in `CharacterMotor`. Match on name suffix across the
whole runtime.

## Rules core — the one part that is genuinely done

`Packages/com.tumbangpreso.core/` — engine-free C#, 32 tests green. Every constant
transcribed from the .gd, NOT from `Design.md` (which has drifted; see
`Design_Drift_Report.md` — all 4 discrepancies were stale prose, the code is right).
