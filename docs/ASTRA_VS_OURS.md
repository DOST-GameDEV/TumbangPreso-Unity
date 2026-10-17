# ASTRAReworks against our line: what to design on

Written 2026-10-17. Compared: `origin/ASTRAReworks` at `a7c1189` (2026-10-17 20:24) against this worktree (`QoLUpdates`
at `eb2dc15` plus all uncommitted effects and cutscene work). ASTRA already contains every QoLUpdates commit; it is 441
commits ahead. Nothing was merged or changed to write this.

**The plan this is for (owner, 2026-10-17):** take each hero's ability KIT from ASTRA (what the abilities do, their names,
numbers and rules), then rework their EFFECTS the way we have been redesigning the old ones (`docs/VFX_BENCH_METHOD.md`:
references first, built from the approved bench pieces, side by side, locked on approval, GPU and measured, honest
footprint, readable in daylight without bloom).

## 1. Where the two lines differ, hero by hero

| Hero | Kit (what the abilities do) | ASTRA's effects | Ours | Overlap |
|---|---|---|---|---|
| Dante / Basilio | **Changed on ASTRA**: Concussed is 5 s (ours 2.5 s); Continental Drift reworded as bands of earth that knock players away. The Earthbound passive rule is the same on both; ASTRA gave it a look | Rebuilt 16 to 17 Oct, kit v4 and cutscene v8 | Boulder approved and in game; old cutscene p11; drift look round two | **Heavy** |
| Nemu | **Replaced on ASTRA**: Kuro's four commands are gone; three ghosts and LET THEM IN | Rebuilt 11 to 17 Oct, ultimate v17 | Old Kuro kit; cutscene p16 (LIGHTS OUT); rage Kuro remodel | **Total**: our Nemu work is for a kit ASTRA retired |
| Amihan | Same kit, except our Whirlwind rule (circle, radius 2.1 m) | Only her ultimate's hand-back after the cutscene | Whirlwind final; Drift streak in game | None |
| Zack | Same | None | Quick Circuit in game | None |
| Rafi | Same | None | Water Wall built, switch off | None |
| Sean, Cheska, Phaister, Paete | Same | Paete's cutscene: only where play picks up after it (the ultimates' hand-back commit) | Cutscene polish rounds | Small |

## 2. Dante, ability by ability

| Ability | The kit to follow (ASTRA) | ASTRA's effect now, and your verdicts on it | Ours now | Suggested starting point for our rework |
|---|---|---|---|---|
| EARTHBOUND (passive) | Shoves him half as far | Heel furrows along the real slide, kicked grit, a lean into the shove; crumbles after 2.5 s | Nothing | New: furrows are a ground decal plus small cloud puffs (pooled) |
| UNSTOPPABLE | Cleanse, 15 s Status Immunity | Orbiting shields and fitted armour (approved earlier, "keep exactly"); statuses shatter off as ice and lightning; plates assemble feet up; refusal splash; cooling warning; crumble | Nothing | Keep the approved shields and armour; rework only the shatter, refusal and crumble with bench pieces |
| BOULDER | Held slipper loaded 15 s; next hit Concussed **5 s** | 13 basalt chunks on heel, rims, sole and toe, strap left showing, molten seams; grit rises into his palm; crust closes in 3 steps; cools and flakes; grit trail; bursts on a hit. Your verdict on v1: *"what even is this it looks horrible"*; v2 made it real stone | **Approved and in game**: 4 rocks carved and baked the Lagoon way, a covering 6-rock version for everyone but him, cloud dust, burst on hit, edge-noise board waiting on your pick | **Two finished looks exist. Choose one.** Ours follows the method and is optimised; theirs has your 17 Oct notes. Do not let a merge pick it |
| BASTION | Stone force field 7.5 s, follows him, reflects slippers | Seven waist-high basalt slabs and a see-through heat field, molten rim, clear middle. Verdicts: *"make the hologram look more like its from earth and magma"*, *"u js put 2d drawings on the hologram"*, kit v4 *"nice better ... refine it even more"* | Nothing (not started) | Rework from v4: slabs as Lagoon-way rocks like Boulder's; the field as a ribbon-shader sheet with a molten rim |
| CONTINENTAL DRIFT (ult, live) | He slams; five bands of earth erupt down the lane, knock everyone away; they land Concussed 5 s | Each band a row of six lit basalt slabs with molten cracks, dust at the front, cracks on the exact hit edges; rubble sinks back. Verdicts: *"Throw them in play too"*, *"It doesnt feel like it hits that hard"*, *"kinda boring to look at compared to other characters"* | Old blast look; a "longhorn fist on each blast" round never approved | Rework the bands with the Boulder rock family, cloud dust bursts, honest band footprint |
| CONTINENTAL DRIFT (cutscene) | Ends as the ground rises toward them; play carries on from that frame | v8: THE LEAP then THE CHARGE-UP, both fists, harder knock, ground strikes. Verdicts: *"i like how ult cutscene looks liek rn"*, then *"refine visual feel and presentation"*, *"It doesnt feel like it hits that hard for anyone"* | p11 (the longhorn version) | Take ASTRA's v8 staging; rework its effects and props by our method |
| CONCUSSED (status) | 75% slower, 5 s | Victim: ochre rim, double image, muffle; observers: three basalt pebbles round the head | Nothing | Pebbles from the Boulder rock family |

## 3. Nemu, ability by ability

| Ability | The kit to follow (ASTRA) | ASTRA's effect now | Ours now | Suggested starting point |
|---|---|---|---|---|
| DON'T LOOK (replaces KURO: SIT!) | Hide a ghost; first to come near is blinded 2 s, then slowed (fading over 5 s); lasts 30 s | Blindfold ghost; the blind is its cloth over your eyes, voxel dark, twitching and tearing; no name tags while blind; v6 | Kuro sit (retired) | Rework the ghost body and the blind; the blind is a screen effect, so it must stay cheap and readable |
| SKITTER (replaces KURO: FETCH!) | A ghost fetches your loose slipper, waits until you take it; the taya makes it drop | Ghost holding the slipper in its claws, arms, its own cast and icon | Kuro fetch (retired) | Rework the ghost and its trail |
| LOOKOUT (replaces KURO: CATCH!) | Lantern ghost guards the can 15 s; lifts it back up once; attackers scare it off | Lantern ghost heaves the can up; lantern light | Kuro catch (retired) | Rework the ghost and the lantern glow |
| DEAD WEIGHT (passive) | Slows those near, 0.9 speed within 3 m | Passive in the TAB tray | Nothing | Small area tell |
| LET THEM IN (ult, replaces KURO: HAUNT!) | Every other player Possessed 15 s; the soul drags steering toward danger | Possession as an aura of black smoke; cutscene v17 THE SNAP (trip, ghost plunges in, he walks at the lens, glitch, head snap) | LIGHTS OUT cutscene p16; rage Kuro remodel | **Our Nemu ult work is for the retired Haunt.** ASTRA's possession smoke is a direct candidate for the approved cloud particles |

## 4. Things the merge itself will need

- **Network version:** ASTRA 169, ours 159 (the Whirlwind rule). The merged game must take one number above both (170+),
  and ASTRA's Concussed 5 s plus our Whirlwind circle both go in. Windows and Android rebuilt together.
- **Six files conflict as text:** `HeroIntroductionScene.Dante.cs`, `HeroIntroductionScene.Nemu.cs`, `DanteHeroKit.cs`,
  `DanteDriftVisual.cs`, `NetSession.cs`, `ChatAndLobbyChromeTests.cs`. Following ASTRA's kit means taking ASTRA's side of
  the first four.
- **Boulder merges WITHOUT a conflict and silently swaps looks:** our `DanteBoulderStone` hooks in front of ASTRA's
  rewritten `DanteBoulderCoating` and turns it off. Decide which Boulder ships; then set `DanteBoulderStone.Rocks`
  accordingly or remove the loser.
- **Our Nemu cutscene, our rage Kuro remodel and the old Dante cutscene p11** become dead if ASTRA's kits are followed.
  Keep them as records in `tools/cutscene_rework/old/`; do not delete.
- Everything else of ours (Whirlwind, Drift, the cloud and landing dust, Quick Circuit, Water Wall, the bench and its
  approved backups) does not collide with ASTRA.

## 5. Decisions only the owner can make

1. ~~Boulder: ours or ASTRA's~~ **DECIDED 2026-10-17: ours.** After a side-by-side film of both on the same court (`Logs/paete-ability-film/danteboulder_versus_v1.mp4`), the owner: "we're still keeping our design". On the merge, ASTRA's rewritten `DanteBoulderCoating` stands down behind `DanteBoulderStone.Rocks = true` deliberately. ASTRA's own `DanteBoulderCoating` does NOT carry our guard (`bool loaded = !DanteBoulderStone.Rocks && ...`), so the merged coating must keep that guard and `Slipper.cs` must keep calling `DanteBoulderStone.TrackWorld`; otherwise both looks draw on the same shoe.
2. Concussed 5 s: accepted with ASTRA's kit, which also changes Boulder's and Drift's text.
3. Which Dante and Nemu pieces to rework first, and whether ASTRA's approved Unstoppable shields stay exactly as they are.
4. Where the work happens: rework on top of ASTRA (merge first, then design), or keep designing here and merge later.
   Merging first is safer: every rework then starts from the kit it will ship with.
