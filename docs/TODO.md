# TODO: Tumbang Preso Unity

Open work, ordered by what is worth doing next. Each entry says what is wrong, where it lives,
and what "done" looks like, so nobody has to re-derive it.

**Check this before inventing a task, and update it in the same commit as the work.** Finished
items move to **Closed** at the bottom with one line on how they were verified.

Read [`VISION.md`](VISION.md) first if you have not. Several entries here only make sense
against the readability budget in its § 2.

---

## 1 · The ability VFX are puddles. Smaller, more detailed, with real particles

**This is the one live item, and it is the next session's whole job.** Everything else in the
open list is smaller.

**Symptom.** *"It just looks like puddles everywhere, they're all too big."* From a live
build: a flat magenta plane covering most of the road, a purple plane under it, a yellow disc
on top of both, and a solid ice wall filling the left third of the screen. Nothing reads.
Also: *"i also want like actual special effects like idk particles? for some skills"*.

**Big skills are fine. Every skill being big is not.** *"its okay for there to be big skills
but not every single skill should be big bruhh, esp that ice shit its so big."*

### 1.1 It is a footprint problem before it is an art problem, and it is measurable

The box is `CONFINEMENT_RADIUS` **7.0**, so the danger zone is **14 x 14 = 196 sq m**.

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
box, they read instantly, and nobody has complained about them. The three offenders are
**Cheska, Nemu and Dante**. Two Permafrost Sheets already cover 80% of the arena, and Cheska's
ultimate deflects slippers from outside the box.

### 1.2 It already costs gameplay, measured

`AiTuning.HazardAvoidMaxRadius` is **3.0**, and it exists only because of these numbers. Bots
now steer around hero hazards, but there is no way round a disc covering half the arena, so
anything wider is walked straight through. Turning avoidance on without that cap took
`BotBehaviourProbe`'s Hero Strike run from **59 throws and 122 skill uses down to 11 and 3**,
with 661 unretrieved-slipper penalties: every bot was surrounded by ground it was correctly
refusing to cross, and simply stopped playing.

**When the footprints come down, that cap stops mattering and the avoidance starts applying to
every hazard with no further code change.** That is the intended end state.

### 1.3 Direction

Shrink the footprint, spend the saved budget on detail and particles.

- Nothing but an ultimate should exceed roughly **2.5 m of radius**. Skills belong in the
  1.8 to 2.5 band Sean and Zack already occupy.
- Ultimates may be big, but **one at a time**. Glacial Nova paints the floor twice: a 6.5
  residual sheet on top of its own 7.5 freeze. Pick one.
- **Replace the flat coloured planes.** A single unlit quad at 40% of the arena is exactly what
  reads as a puddle. The same silhouette at 2.2 m with a cracked edge, a rim, depth and
  particles reads as ice.
- **The floor is not the only place to put an effect.** Verticality, edge treatment and short
  bursts cost no floor area at all.
- **Cap the stacking.** Two translucent planes plus a disc plus a wall plus four popup labels
  is not four effects, it is one unreadable frame.
- **Cheska first.** She is the one he named.

### 1.4 Particles: where they belong

There are none today. Every effect is built from primitives and unlit materials in
`HeroHazards.cs`; the only particle-like things in the game are `ComicPopup`, `DizzyStars` and
`SpawnConfettiShower`, which are UI-ish rather than world VFX.

⚠️ **DECIDE THE HOME BEFORE WRITING ANY.** The suggested split, to be confirmed by whoever
does the work:

- **A new `Assets/TumbangPreso/Runtime/Visual/AbilityVfx.cs`**, alongside the existing
  `Visual/` effects, owning every `ParticleSystem` an ability spawns. `HeroHazards` keeps the
  hazard's SHAPE and its GAMEPLAY volume; `AbilityVfx` owns what it looks like. That split is
  what stops the next footprint change from being an art change.
- **Built in code, cached, like every other surface in this project.** `GodotTheme` bakes every
  UI sprite and `AbilityIcons` bakes every glyph, both for the reason `GodotTheme` records: a
  baked asset that drifts from the code that wanted it is indistinguishable from a broken
  conversion. A `ParticleSystem` authored in a prefab is a fair exception if the team wants to
  art-direct it, but say so in the file.
- **Warm them in the boot preload.** `SplashScreen.PreloadGameAssets` has a numbered list and
  a note saying anything that can hitch warms there. A first-cast particle burst that compiles
  a shader mid-round is exactly that.
- **Budget them.** Four players casting at once in a 14 m box; a system that looks good alone
  and unreadable in a fight has failed § 2 of `VISION.md`.

### 1.5 Also in the same pass

The stun frost (`Assets/TumbangPreso/Shaders/FrostVignette.shader`) was cut from 0.36 to 0.24
screen heights of reach this session, which took the clear centre from 0.28 to 0.52 of the
frame. **Look at it in a real match before deciding it is settled**; it is a judgement call
and the arithmetic only says it is no longer covering three quarters of the screen.

### 1.6 Done when

No single skill covers more than about a tenth of the box, no two floor hazards at once cover
more than a third of it, a screenshot taken mid-fight still shows the lata, the chalk and every
player, and the skills that should feel big have particles rather than area. Take the
screenshot from the built player and put it in the reply.

**Where.** `Assets/TumbangPreso/Runtime/Abilities/*HeroKit.cs` for the radii,
`Assets/TumbangPreso/Runtime/Abilities/HeroHazards.cs` (1126 lines) for the geometry and
materials each `Spawn*` builds.

---

## 2 · Close the 8 PARTIAL rows in `docs/Port_Ledger.md`

Zero MISSING rows remain. The eight partials, with what is actually left on each:

| File | What is missing |
|---|---|
| `audio_manager.gd` | Bus layout, mix levels, transitions, voice triggers (N15) |
| `round_manager.gd` | Per-round transition polish |
| `match_manager.gd` | Ranking and defender derivation edge cases |
| `debug_player_switcher.gd` | Beyond seat drive, cycle and readout |
| `character_base.gd` | Third-person charge pose (N14) |
| `ai_controller.gd` | Per-plan polish (N18) |
| `match_result.gd` | Peer rematch voting across the wire |
| `HUD.tscn` | Resolution of N17 |

⚠️ **`audio_manager.gd` is the biggest single win here** and the most player-visible: mix
levels and voice triggers are what make a match feel produced rather than assembled.

---

## 3 · Reconnect is verified in simulation, not across two processes

`LobbyAndSettingsTests` and `RuntimeLayerTests` cover token reclaim, seat restoration and the
exact "dropped as attacker, returns as taya" case end to end; `NetworkMultiProcessProbes`
covers topology. **Two real processes over a LAN has still never been run.** Before any
bracket play, it has to be.

---

## 4 · The IKE slipper still carries the real Nike wordmark as geometry

First in the art replacement queue. `docs/Port_Plan.md` § 8 has the order and, more
importantly, the list of properties a replacement must preserve, because several props were
tuned against the exact shape that was drawn.

---

## Closed

- **Load every resource on the BH Studios loading screen.** ✅ 2026-08-23. The preload covered
  the roster, audio and the MAIN MENU scene, and then the arena, its materials, the baked UI
  sprites and the hero kits were all still cold when Play was pressed. It now also warms both
  maps through their assets, every `GodotTheme` box, every ability glyph, the input asset with
  the player's rebinds, and all five kits. `SplashScreen.PreloadGameAssets`.

- **Plan the whole keymap and put throw on left click.** ✅ 2026-08-23. Throw always WAS on
  left click; Grab was on it too, which is why it did not behave like it. There were four live
  collisions in total (left click carried Throw and Grab, E carried Grab, Lunge and Skill 1,
  Q carried Throw and Skill 2). Every action now owns exactly one control:
  left click throws or punches, E is the contextual pick up / shove / reset, right click
  lunges, Q and F are the skills, X is the ultimate, Tab holds the ability panel open.
  `InputMapAndAbilityTests` asserts no control is shared and that throw is on left click.

- **Redesign the skill UI.** ✅ 2026-08-23. Cards are an icon tile with the bound key on a chip
  in the corner; cooldowns drain a smooth bar and the ultimate fills a notched one, so the two
  quantities can no longer be confused; the deck moved out from under the practice prompt; key
  labels come from the live bindings. Descriptions moved off the HUD entirely into a
  hold-to-read panel, and character select now shows every power with its icon, its kind and
  its sentence.

- **Ultimate charging during the ready screen.** ✅ 2026-08-23, and the requirement changed
  mid-flight. Charge now PERSISTS across rounds and is frozen whenever the round clock is not
  running; during the warm-up and the between-round buffer the ultimate is free to cast off a
  practice counter, so it can be rehearsed without spending the meter or earning one by
  waiting. Cooldowns still run in practice, deliberately.

- **Organise the settings controls into groups.** ✅ 2026-08-23. Four headed sections
  (Movement, Playing the game, Hero powers, Round and screen) instead of fourteen unlabelled
  rows. `SettingsGroupsCoverEveryActionExactlyOnce` asserts nothing can fall out of the panel.

- **Hero Strike unretrieved-slipper penalty variance.** ✅ 2026-08-23. Two causes, both
  measured. The probe was unseeded, so the same build measured 110 and then 467 penalties on
  consecutive runs either side of its own 200 ceiling; it is seeded now. And bots walked
  straight through hero hazards on the way to a tsinelas, so `HazardMap` and
  `AIController.AvoidHazards` steer around them. Hero Strike now measures 77 throws, 77
  retrievals, 182 skill uses, 21 ultimates and **1** unretrieved-slipper penalty in a match.
  ⚠️ The avoidance is capped at 3 m until § 1 lands; see § 1.2.

- **The stun frost is very strong.** ✅ 2026-08-23, reduced. Reach 0.36 to 0.24 screen heights,
  body alpha 0.36 to 0.30. Both opposite edges spend the reach, so at 0.36 the clear strip left
  in the middle was 0.28 of the screen height for a five second stun. Still worth a look in a
  real match: § 1.5.

- **The preview idle pose vs the Godot reference.** No need. The character preview was reworked
  in a separate pass; the arms-crossed mismatch in `ModelPreview.PlayIdle` is not being chased.
