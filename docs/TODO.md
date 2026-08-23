# TODO: Tumbang Preso Unity

Open work, ordered by what is worth doing next. Each entry says what is wrong, where it lives,
and what "done" looks like, so nobody has to re-derive it.

Closed items move to the bottom under **Closed** with one line on why.

---

## 1 · Preload everything on the BH Studios loading screen

**Symptom.** Clicking Play stutters. The loading screen finishes, then the game loads the rest
of the world on the first frame of the match.

**Where.** `Assets/TumbangPreso/Runtime/UI/SplashScreen.cs`, `PreloadGameAssets()`.

**What it covers today.** `Shader.WarmupAllShaders()`, `RosterBook` (people, clips, palettes,
pets, cans, slippers), every `AudioClip` under `Resources`, the settings and roster tables, and
`SceneManager.LoadSceneAsync(SceneFlow.MainMenu)` held at 90% with `allowSceneActivation = false`.

**What it does not cover, which is the lag.**
- The arena scene itself. Only the main menu is pre-loaded, so the match scene loads on click.
- Map geometry and materials for the map the player is about to pick.
- Hero ability prefabs and their VFX and hazard materials
  (`Assets/TumbangPreso/Runtime/Abilities/`, `HeroHazards`). First cast of every skill in a
  match compiles and instantiates cold.
- HUD and overlay canvases (`Hud.cs`, `ConvertedOverlay.cs`).
- `Shader.WarmupAllShaders()` does not warm variants that are only reachable from a material
  the arena creates at runtime. A `ShaderVariantCollection` authored from a real match is the
  real fix.

**Done when.** A cold launch into a Hero Strike match shows no frame over the budget between
pressing Play and the first ready prompt, measured, not eyeballed. The six-second slow-load
warning is already in there to tell us when the preload got too heavy.

---

## 2 · Plan the whole keymap once, on paper, then apply it

**Symptom.** "There are so many conflicts." Correct, and here they are in full. Defaults live in
`Assets/TumbangPreso/Resources/TumbangPreso.inputactions`.

| Action | Label | Bound to |
|---|---|---|
| `SpecialAbility` | Throw | **LMB**, **Q** |
| `Grab` | Grab | **E**, **LMB** |
| `Lunge` | Lunge | **E**, RMB |
| `Skill1` | Skill 1 | **E** |
| `Skill2` | Skill 2 | **Q** |
| `Ultimate` | Ultimate | F, Z |
| `Sprint` / `Jump` | | LeftShift / Space |
| `EmoteWheel` / `ReadyUp` / `CleanFeed` / `ToggleFullscreen` | | B / R / H / F11 |
| `SpectatorDown` | not rebindable | LeftCtrl |

**The four real collisions.**
- **LMB carries both Throw and Grab.** Throw *is* already on left click; it does not feel like
  it because Grab is on the same button and whichever consumes the press first wins. The wanted
  behaviour is: left click throws when holding a tsinelas, grabs when not.
- **E carries Grab, Lunge and Skill 1.** Three verbs, one key. The HUD shows `[E] SEISMIC STOMP`
  while E is also the contextual pickup and the taya's tag.
- **Q carries Throw and Skill 2.** The HUD shows `[Q] DEMONIC CARAPACE`.
- **`Rebinding.TryRebind` refuses any key already in use and names the conflicting action**
  (`Runtime/Settings/Rebinding.cs`), so the shipped defaults violate the rule the rebind panel
  enforces. Rebinding anything onto E or Q is currently refused by our own asset.

**Also.** `SpectatorDown` is bound but missing from `Rebinding.RebindableActions`, so it has no
row in the settings panel. `Ultimate` has two keys (F and Z) with no stated reason.

**Done when.** One table in this file is the source of truth, the `.inputactions` asset matches
it, `Rebinding.RebindableActions` and `ActionLabels` match it, and a fresh profile has zero
duplicate paths across actions. Left click throws.

---

## 3 · Redesign the in-match skill UI

**Symptom.** The three ability cards along the bottom read as confusing. From the current build:
`[E] READY / SEISMIC STOMP`, `[Q] READY / DEMONIC CARAPACE`, `[F] 6% / DEMON TITAN FISSURE`.

**Problems visible in that one frame.**
- Two cards say READY and one says a percentage. Cooldown and ultimate charge are two different
  quantities rendered in the same slot, so the eye cannot tell a ready skill from a charging ult.
- The key hint sits top-left and the state top-right, so reading one card takes two saccades.
- Ability names are long and wrap to two lines at the card width.
- The cards sit under the centre-screen instruction text and compete with it.
- No icons. Names alone do not survive a glance mid-round.

**Where.** `Assets/TumbangPreso/Runtime/UI/Hud.cs`.

**Done when.** There is a decided layout (sketch or reference first, then code), cooldown and
ult charge are visually distinct at a glance, nothing wraps at any of the nine resolutions
`AspectRatioProbes` covers, and the cards do not overlap the centre prompt.

---

## 4 · Ultimate charge must reset each round and on the R press

**Decision.** Charging during the practice or ready screen is **fine, and stays for testing**.
The bug is that it never clears.

**Confirmed in code.** `HeroAbilitySystem.ResetKit()`
(`Runtime/Abilities/HeroAbilitySystem.cs:186`) calls `Kit?.Reset()`, and **`ResetKit` has no
call sites anywhere in `Assets` or `Packages`.** Nothing zeroes `HeroKit.UltimateCharge`
between rounds. `HeroKit.Tick` trickles `Balance.UltimatePassiveChargePerSecond` every frame the
kit ticks, practice time included, so charge carries across the round boundary and a player can
open round 2 with an ultimate banked from round 1's warm-up.

**Where to hook it.**
- `SliceRunner.ResetWorld(defenderSlot)` (`Runtime/SliceRunner.cs:224`) is called from both
  `OnRoundStarted` and `OnIntermission`, so it covers every round transition.
- The R press path is `ReadyGate.RoundShouldBegin` to `runner.Begin`
  (`Runtime/MatchInstaller.cs:472`). `Begin` is documented as idempotent, so a reset there is safe.

**Done when.** A test asserts charge is zero at the first frame of every round and immediately
after R starts the round, for all four seats.

---

## 5 · Close the 8 PARTIAL rows in `docs/Port_Ledger.md`

Zero MISSING rows remain. The eight partials:

`audio_manager.gd` (bus layout, mix levels, voice triggers) · `round_manager.gd` ·
`match_manager.gd` · `debug_player_switcher.gd` · `character_base.gd` (third-person charge pose,
N14) · `ai_controller.gd` (per-plan polish, N18) · `match_result.gd` (peer rematch voting across
the wire) · `HUD.tscn` (N17).

---

## 6 · Hero Strike unretrieved-slipper penalties still vary run to run

0 to 28 across the last measurements, against 205 before the fetch tune. The residual cause is
attackers pathing around hero hazards with a straight-line steer. Real path avoidance around
`HeroHazards` colliders is the fix.

`BotBehaviourProbe`'s ceiling is 200 with the reasoning written out. The liveness floors (throws
and retrievals above 20) are the assertions that actually matter.

---

## 7 · The stun frost may be too strong

A Cheska ice tag whites out most of the screen for 4.6 s. It has its own tests and is
deliberate, but it is close to the unreadable-effects line. Judgement call, not measured.

## 8 · The ability VFX are puddles. Smaller and more detailed, not bigger

**Symptom.** *"It just looks like puddles everywhere, they're all too big."* From the current
build: a flat magenta plane covering most of the road, a purple plane under it, a yellow disc on
top of both, and a solid ice wall filling the left third of the screen. Nothing reads. **Big
skills are fine. Every skill being big is not.**

**This is a footprint problem before it is an art problem, and it is measurable.** The box is
`CONFINEMENT_RADIUS` **7.0**, so the danger zone is **14 x 14 = 196 sq m**. Against that:

| Ability | Footprint in code | Share of the box |
|---|---|---|
| Cheska · Permafrost Sheet | radius **5.0**, 6.0 s | **40%** |
| Cheska · Ice Barricade | width **6.5** | **46% of the 14 m edge** |
| Cheska · Glacial Nova, residual sheet | radius **6.5** | **68%** |
| Cheska · Glacial Nova, freeze radius | **7.5** | **90%** |
| Cheska · Glacial Nova, slipper deflect | **8.5** | **116%, wider than the box itself** |
| Nemu · Seance Void | radius **7.5**, 5.0 s | **90%** |
| Dante · Cracked lava decal | radius **5.5**, 4.0 s | **48%** |
| Sean · Fire Trail | radius **1.8** | 5% |
| Zack · Shock Trail | radius **2.2** | 8% |

⚠️ **SEAN AND ZACK ARE THE PROOF THAT THE TARGET IS RIGHT.** Their trails are 5% and 8% of the
box, they read instantly, and nobody has complained about them. The three offenders are Cheska,
Nemu and Dante. **Two Cheska sheets already cover 80% of the arena, and her ultimate deflects
slippers from outside the box.**

**Direction, in one line: shrink the footprint, spend the budget on detail inside it.**

- Nothing but an ultimate should exceed roughly **2.5 m of radius**. Skills belong in the
  1.8 to 2.5 range Sean and Zack already sit in.
- Ultimates may be big, but **one at a time**. Cheska's residual sheet at 6.5 on top of her own
  freeze at 7.5 is one cast painting the whole floor twice.
- **Replace the flat coloured planes.** A single unlit quad at 40% of the arena is what reads as
  a puddle. Same silhouette at 2.2 m with a cracked edge, a rim, some depth and particles reads
  as ice.
- **The floor is not the only place to put an effect.** Verticality, edge treatment and short
  bursts cost no floor area at all.
- **Alpha and additive stacking is what killed readability in the screenshot.** Two translucent
  planes plus a disc plus a wall plus four popup labels. Cap what can overlap.
- Cheska's ice is the worst of it and should be done first.
- Related: the stun frost in § 7 whites out most of the screen for 4.6 s. Same complaint, same
  fix, and worth doing in the same pass.

**Where.** `Assets/TumbangPreso/Runtime/Abilities/*HeroKit.cs` for the radii,
`Assets/TumbangPreso/Runtime/Abilities/HeroHazards.cs` (1126 lines) for the geometry and
materials each `Spawn*` builds.

**Done when.** No single skill covers more than about a tenth of the box, no two hazards on the
floor at once cover more than a third of it, and a screenshot mid-teamfight still shows the
lata, the chalk and every player.

---

## Closed

- **Preview idle pose vs the Godot reference.** No need. The character preview was reworked in a
  separate pass; the arms-crossed mismatch in `ModelPreview.PlayIdle` is not being chased.
