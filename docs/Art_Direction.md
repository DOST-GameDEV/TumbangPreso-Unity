# Art Direction — the laws

## 1 · The colour law — the one rule that never bends

**Orange `#f87020` means OFFENSE. Blue `#0080e8` means DEFENCE.** They track the ROLE,
which swaps every round; they never mean a team, a character or decoration. Nothing else in
the frame — no outfit, no can tint, no slipper strap, no environment piece — may sit near
those two hues. Every other palette decision is yours to make; record it and move on.

`ui_theme.gd` is the only place a colour is named. Read it, never restate it.

| Band | Members |
|---|---|
| UI | `INK` `PANEL` `CARD` `OFFENSE` `DEFENSE` `IMPACT` `HIGHLIGHT` `DANGER` |
| Menu wood | `WOOD_DEEP` `WOOD_MID` `WOOD_DARK` `WOOD_EDGE` `CREAM` `AMBER` |
| Environment | `ENV_*` — asphalt, concrete, GI sheet, rust, wood, foliage, dirt, tarp, rubber, four facade paints |
| Hero props | `PROP_FOAM` `PROP_FOAM_DARK` `PROP_WEBBING` `PROP_SARSI_RED` |

A hero prop **may** be more saturated than any `ENV_*` — it is the most-looked-at object in
the game and has to read against asphalt. It may not approach the role hues.

## 2 · The scale and height laws

| Fact | Value | Why it is a law |
|---|---|---|
| Person capsule | r 0.40, h 1.60 | eye at 1.25 |
| Person model scale | 2.38 | measured off the Kenney rig's AABB, not guessed |
| Person model yaw | +180° **on the MODEL node only** | the rig's face is on +Z, Godot's forward is −Z. **Never rotate the body to fix a render.** |
| Lata capsule | r 0.14, h 0.34 | knee-high |
| Tsinelas visual scale | **1.60** (was 1.25) | bigger for drama; the capsule scales with it or you get a slipper you can see and cannot step on |
| Tsinelas capsule | r 0.256, h 0.512 | = the 1.25-era row × 1.28 |
| Interior clutter | **≤ 1.0 tall** | a jump apexes at 0.841; anything taller becomes a platform |

## 3 · Arena geometry

| Mark | Value | Source of truth |
|---|---|---|
| Base circle | ring at r **0.70**, world origin | `env_kit.gd::_base_circle_decal` |
| Lata "home" for the countdown | r **0.9** | `Design.md` §5.2 |
| Confinement marker | **SQUARE** at \|x\| = \|z\| = **6.5** | `CharacterBase.CONFINEMENT_RADIUS`, parsed out of the .gd by both map builders — **do not reshape that `const` line** |
| Throwing line | **7.5** from centre (= box + 1.0), one per side | `throwing_line_decal`, position derived from the box in both builders |

⚠️ **THE BOX WENT 5.0 → 6.5 ON 2026-08-01** (human ask, twice). The court width follows it
automatically — `COURT_X = CONFINEMENT_BOX_RADIUS` — but the throwing line did **not**: it
was a literal `6.0`, which the widened box swallowed, putting the chalk mark 0.5 inside
the area the throw gate refuses from. It is derived now. `Design.md` §2 carries the
reasoning for the value.
| Team-side line | 6.0 × 0.08, `PANEL` at 40 % | subordinate to the throwing line on purpose |

A square and a circle of the same "radius" agree only at the four edge midpoints; on the
diagonals they differ by 2.07 units. The physics clamps X and Z independently to match the
drawn square.

## 4 · The models — where every asset comes from

⚠️ **ONE OUTPUT PATH, ONE PRODUCER. This is the rule that has broken twice.**
`generate_all.gd` silently overwrote the sourced slippers for hours because both
wrote the same filenames, and `build_prop_textures.py` was still writing three of
`build_footwear.py`'s texture files when that was found on 2026-08-01. If you add a
generator, give it paths nothing else writes.

| Asset | Produced by | Run it with |
|---|---|---|
| The six **cans** (`lata_*.obj`) | `tools/build_lata.py` | `python tools/build_lata.py` |
| The viewmodel arm + the whole `env_kit` | `tools/models/generate_all.gd` (GODOT REPO, FROZEN) | see below |
| The four ORIGINAL **can textures** (`textures/lata_*.png`) | `tools/models/build_prop_textures.py` | `python …` (needs Pillow) |
| Any **can texture**, matte removed | `tools/strip_texture_border.py` | `python tools/strip_texture_border.py <png…>` |
| The ten **slipper skins** (`tsinelas_*`) | sourced GLBs plus `tools/build_slipper_models.py` | Blender batch mode, see the script header |
| A **prop already in the repo**, put into the carry frame | `tools/normalise_obj_prop.py` | `python tools/normalise_obj_prop.py <obj>` |

⚠️⚠️ **`tools/build_slipper_roster.py` AND `tools/models/glb_tool.py` NO LONGER EXIST.** The
first was deleted on 2026-08-28 once its last row, PAMBAHAY, took a new source; leaving it
would have meant two generators writing the same `tsinelas_<id>.glb` filenames, which is the
fault `generate_all.gd`'s own header records costing a session. The second was already gone
before that, which is why `tsinelas_sike.mtl` is maintained by hand and says so.

⚠️ **`generate_all.gd` lives in the FROZEN Godot repo and must not be run or edited.** That is
why the can builder was ported to `tools/build_lata.py` rather than invoked: a fifth can could
not otherwise be added. That port is proved by `--verify`, which rebuilds all four original
cans and compares them line by line against the files in this repo.
| The twelve **Person palettes** (`person_*.tres`) | `tools/models/generate_person_palettes.py` | `python …` |
| Both **maps** | `tools/maps/build_*.py` | `python …` |

**Acceptance test for every one of them: run it twice and `git status` must be clean.**

Everything in the world is CC0 Kenney (City/Suburban, Fantasy Town, Mini Forest, Food,
Furniture, Car) plus the project's own generated `env_kit` decals.

### 4a · The slipper roster — SOURCED MODELS, NOT THE DRAWINGS

⚠️ **THE DRAWING-DERIVED SLIPPERS ARE DELETED AND MUST NOT BE REBUILT.** 🧑
2026-08-01, shown the old drawing sheets: *"yo thats old stale stuff · dont"*, and
earlier: *"i think u gen js suck in 3d modelling ahah … js look for assets that look
like them"*.

Four procedural slippers were built from `docs/refs/props/tsinelas_sheet*.png` and
rejected four times on look. They are gone: `tsinelas_bakya.*`, `tsinelas_tsinelas.*`
and their textures were deleted on 2026-08-01, along with `build_prop_textures.py`'s
whole slipper half. **The sheets survive in `docs/refs/` as history only — nothing
reads them.**

What ships is three sourced CC-BY models converted by `build_footwear.py`, plus the
project's own original mesh:

| Roster id | Mesh | Origin |
|---|---|---|
| `tsinelas` | `tsinelas_tsinelas.glb` | Tiff Eidmann, CC-BY-3.0, rebuilt 2026-08-28 |
| `crocs` | `tsinelas_crocs.obj` | sourced, CC-BY — see §4b |
| `pantulog` | `tsinelas_pantulog.obj` | sourced, CC-BY — see §4b |
| `sike` | `tsinelas_sike.obj` | sourced, CC-BY — see §4b |
| `spartan` | `tsinelas_spartan.glb` | Poly by Google, CC-BY-3.0 |
| `alpombra` | `tsinelas_alpombra.glb` | Isa Lousberg, CC0-1.0 |
| `pambahay` | `tsinelas_pambahay.glb` | Tiff Eidmann, CC-BY-3.0 |
| `heels` | `tsinelas_heels.glb` | jeremy, CC-BY-3.0 |
| `sandals` | `tsinelas_sandals.glb` | jeremy, CC-BY-3.0 |

All nine are normalised to **0.432 m** toe-to-heel and centred on their volume
centroid, so `Slipper.HIT_RADIUS` and the two-axis spin hold for every skin.
`tools/models/skin_probe.gd` gates all eight prop skins; `tools/models/charprop_probe.tscn`
gates that the CHARACTER screen previews each one's own mesh rather than a shared one.

**A character is a rig plus a palette, never a new model** — twelve Kenney rigs carry twelve
roster Persons through `person_palette.gdshader` and a generated `.tres`
(`tools/models/generate_person_palettes.py`). Do not hand-edit a `person_*.tres`.

**A PROP SKIN IS A MESH, AND SINCE 2026-08-01 THAT IS THE WHOLE OF IT.** This law used
to read *"a tint plus ATTACHMENTS, never a new base mesh"* and it is now the opposite:
each `CANS`/`SLIPPERS` entry names its own `model`, `lata.gd`/`slipper.gd` swap the mesh
in `apply_skin()`, and `tint` is **white** on all eight — a no-op that exists so a
coloured variant is still possible later.

> ⚠️ **Anything that shows a prop must swap the MESH, not just recolour.** Three places
> do it and all three must stay in step: `lata.gd::_apply_model()`,
> `slipper.gd::_apply_model()` and `character_preview.gd::_apply_model()` (the CHARACTER
> screen, which for one commit previewed every lata as the same can because it only
> tinted). `camera_rig.gd::_sync_viewmodel_slipper()` is the fourth — the first-person
> held slipper, which copies the mesh off the world slipper rather than re-deriving it.

⚠️ **THE PROCEDURAL ATTACHMENT KIT IS DEAD CODE.** `character_visual.gd`'s
`SLIPPER_ATTACHMENTS` is keyed by roster ids (`sabit`) that no longer exist, so nothing
reaches it. Left in place, unreferenced; delete it when something else touches that file.

## 4b · Third-party assets and credit — **read before shipping**

The original four sourced models are **CC-BY-4.0**. The five sources added on
2026-08-28 are CC-BY-3.0 or CC0 and are recorded in `NEW_SLIPPER_LICENSES.txt`.
Attribution licences require the author to be credited. Each source ships with a licence file
beside it in `assets/models/kits/footwear/`, exactly as delivered — do not delete
those files, they are the licence compliance.

| Prop | Model | Author | Source | Licence |
|---|---|---|---|---|
| TSINELAS | — | *this project* | `tools/models/generate_all.gd` | project's own |
| CROCS | "crocs" | **fnk** | [sketchfab.com/3d-models/crocs-fbede59e…](https://sketchfab.com/3d-models/crocs-fbede59e03394e928ed0eccf27e8fc23) | CC-BY-4.0 |
| PANTULOG | "Pink Slipper" | **The Withered Rose** | [sketchfab.com/3d-models/pink-slipper-af5b6388…](https://sketchfab.com/3d-models/pink-slipper-af5b6388d4f240389591a4ac09fedf06) | CC-BY-4.0 |
| SIKE | "Low Poly Nike Sandals" | **les03** | [sketchfab.com/3d-models/low-poly-nike-sandals-8e77c949…](https://sketchfab.com/3d-models/low-poly-nike-sandals-8e77c949319148afb9134ba13f64046f) | CC-BY-4.0 |

**The credit has to be REACHABLE, not just written here.** A licence that says
"author must be credited" is not satisfied by a line in a design doc nobody
ships. These four belong on a credits screen or in the submission's asset list
alongside the existing Kenney CC0 and OpenGameArt CC0 credits.
**Filed to 🖥️ `build ui` as § CHECKLIST 1.11.**

> ### ⚠️ THE SIKE CARRIES REAL NIKE TRADE DRESS, AND THAT IS A KNOWN, ACCEPTED RISK
>
> The human's own drawing is a parody — "SIKE" with a swoosh — and the intent was
> to swap the N for an S on the model. **That could not be done.** The model's
> wordmark and swoosh are **geometry, not texture** (the file carries no images at
> all; every colour is a `baseColorFactor`), so changing the letter means editing
> the mesh, which is modelling, which is the Blender step this lane is forbidden.
>
> The model therefore renders an actual Nike swoosh and wordmark. 🧑, told this:
> *"im kinda okay with nike as long as it doesnt make things lag man"*. Recorded as a
> decision, not an oversight — for a competition submission a real trademark on a
> hero prop is a small but genuine risk, and the two ways out are a different
> slide model or deleting the wordmark's faces.

## 5 · The toon pass

⚠️ **"NO TEXTURES" IS NO LONGER TRUE OF THE TWO HERO PROPS, AS OF 2026-08-01.**
This law used to read *"Flat colour, no textures, no UVs"* without exception, and
it still governs the environment and the characters. The lata and the tsinelas
are now textured, on a direct human ruling: they drew four cans by hand — three
carrying readable parody wordmarks (PASIP, BOYBEN, DECADES TUNA) — and
supplied flattened 360° label wraps for the purpose.

⚠️ **Two more arrived on 2026-08-28 and they came the same way**: PIYESTA (Bel Monte
fruit cocktail) and KARNE NORTE (Purofoods corned beef), supplied as flattened wraps at
1774 x 887 and resampled to the 1024 x 512 every other label uses. Both carry parody
wordmarks in the same spirit as the first three. **A new can needs a SILHOUETTE nobody
else has**, not just a new label, because a label is a texture read and dies at arena
distance under the toon bands: PIYESTA is the widest in the set with a deep seam ring at
a third height, KARNE is the only tapered one. 🧑: *"you can use the
flattened art for textures bcz its easier that way, you cant redraw this too
bro"*. Stripped to a flat `Kd`, a Pasip and a Decades are the same grey cylinder
and all of the Filipino specificity is gone with the label.

⚠️ **THE SLIPPERS ARE THE OTHER WAY ROUND AND IT IS NOT AN INCONSISTENCY.** Two of
them (CROCS, PANTULOG) carry a `map_Kd` extracted from their source `.glb`; the
other two carry none at all — `tsinelas_classic` is generated flat colour, and the
SIKE's every colour lives in a per-material `baseColorFactor` (which is why it is a
black sandal with a white swoosh and no image file anywhere). All four therefore
still take `tint` **white**, and the one-material rule below still binds.

**It costs the tint system nothing**, which was the objection. `toon.gdshader`
already had a textured path (added for the Kenney kit atlas), and on it
`albedo_color` MULTIPLIES the sampled texture rather than replacing it — so a
textured skin carries `tint` **white**, which multiplies to a no-op, and
`lata.gd::_tint_meshes()` is untouched and still works for anyone who wants a
coloured variant. `obj_writer.gd` gained UV emission and `map_Kd`; the untextured
path is byte-identical, so every environment mesh regenerates unchanged.

**One material per hero prop, and that is forced.** The tint walk overwrites
every surface it finds, so a second untextured material would be repainted flat
white by a white tint. Where a part needs a different colour it gets it by being
projected onto a different part of the ONE texture — which is why the cans' end
caps are UV-pinned to their wrap's rim bands.

Everything else below still holds:

Flat colour, no PBR. Two shaders: `toon.gdshader` (banded lambert + a
rim term) and `outline.gdshader` (inverted hull). Prop outlines are sized in **world** units
(`OUTLINE_WORLD_WIDTH` 0.012) because meshes differ in scale; Persons take an early-out and
keep `person_outline.tres`. Large environment silhouette pieces get an outline; small
dressing does not.

Hit flash drives a separate `flash_amount` uniform, so writing `albedo_color` for a skin
tint is safe and cannot break the flash.

## 6 · Maps

| Map | Read |
|---|---|
| **Eskinita** | urban side street — sari-sari, sampay, kanal, corrugated walls, 10–14 m rooflines |
| **Bayan Plaza** | barangay plaza — church facade, basketball ring, acacia, monument |

Both are emitted **wholesale** by `tools/maps/build_*.py`. Anything you add to a `.tscn` by
hand survives exactly until the next layout run — edit the builder.

## 7 · Deviations from the moodboard, all deliberate

You do not need permission to diverge from the moodboard. You need to write down that you
did.

| Board said | Build does | Why |
|---|---|---|
| Yellow/blue/orange school outfits | Green sando + maroon top | §1 — those hues mean role |
| Magenta slipper / can accents | `PROP_FOAM` brown and Sarsi livery | superseded by the human's own second prop moodboard: *"the magenta stuff is just placeholder"* |
| 1024² PBR on both props | Flat colour | the pipeline emits no UVs and the stated reference is flat colour |
| Props at hero scale | Real scale, then the 1.6× slipper for drama | the board had no environment to be out of proportion with |

---

## 8 · Character Customization, Roster Integrity & Skin Tones

### 8.1 Canonical Roster Heroes: Fixed Identity Law
The twelve Classic street characters (BERTO, MARING, TOTOY, INDAY, KUYA BOY, ATE GIRLIE, TIKBOY, BEBANG, JUN-JUN, LOLA PACING, MANG KANOR, ALING NENA) and the six Hero Strike heroes (DANTE, CHESKA, SEAN, ZACK, NEMU, PHAISTER) have fixed, non-negotiable character art:

⚠️⚠️ **THE SIX HEROES ARE DANTE, CHESKA, SEAN, ZACK, NEMU AND PHAISTER, AND `Berto` IS NOT ONE OF THEM.** `Roster.HeroPeople` is the list. `bayan`, whose display name is BERTO, is the first of the twelve CLASSIC street characters and has no ability kit at all. Five documents and one code table wrote *"heroes (Berto, Sean, Dante, Cheska, Zack, Nemu, Phaister)"* on 2026-08-31 and `HeroLoadoutRules` then shipped ability sidegrades for Berto while omitting Phaister entirely. `HeroLoadoutTests.EveryVariantBelongsToAHeroTheGameActuallyHas` reads `Roster.HeroPeople` directly now, so the table and the roster cannot drift again. `docs/TODO.md` § 108.3.

- **Skin tones, facial features, hair geometry, and eye shapes are canonical and locked.**
- **No global tint dials or alien hue sliders on classic heroes.** Berto must never be tinted green, cyan, or magenta.
- **Hero Cosmetics are Outfits Only**: Thematic jackets, alternate streetwear, or tournament jerseys that preserve the hero's identifiable silhouette.

### 8.2 Custom Character Creator: 3-Slot Modular System
The dedicated "Create Your Own Character" feature (3 save slots, 1 active) allows deep personalization within the authentic Filipino street universe:
- **Authentic skin tone palette**: **32 warm tones**, from `Porcelain Fair (#FCE7DC)` to
  `Deep Kalye Bark (#4E240D)`, running through `Golden Bronze (#ECAA6C)`,
  `Classic Kayumanggi (#C88A52)`, `Sun-Baked Tan (#DC9E64)`, `Warm Chestnut (#8D5B34)` and
  `Sunlit Peach (#F4C29E)`.
  ⚠️⚠️ **THE LIST LIVES IN `CustomCharacterRules.SkinToneNames` AND THE HEX IS PART OF THE
  NAME**, which is why this document names five of them as landmarks rather than transcribing
  thirty-two. This section said there were five, in a document a reader would reasonably treat
  as the definition, while the code had thirty-two: **a table copied out of a list is a table
  that disagrees with the list the first time somebody appends to one of them.**
  ⚠️ Every one is warm. There is no cyan, magenta or grey in the skin list at all, which is
  the constraint 🧑 asked for, and it is a property of the authored values rather than of a
  filter somebody has to remember to apply.
- **Facial Expressions**: Expressive voxel eyes and mouth decals (chill, determined, fierce, happy, focused).
- **Body and height scaling**: bound to **85 to 115 per cent**, which is
  `CustomCharacter.MinHeightPercent` and `MaxHeightPercent`, so hitbox alignment and jump
  clearances stay competitive.
  ⚠️ **THIS LINE SAID `0.90x to 1.10x` AND THE CODE SAID 85 TO 115, WHICH IS `CLAUDE.md`
  § 5's RULE BROKEN**: *a number in the code must match a number here, or one of the two is a
  bug.* The code is right and the prose was wrong, so the prose moved.
  `RosterIntegrityTests.TheHeightWindowIsTheOneTheDocumentsQuote` is what stops the next
  disagreement being found by a player. ⚠️ **It is bounded at all because
  `CLAUDE.md` § 4 resolves contact by DISTANCE**: reach is the taya's whole job, so an unbounded
  height dial would be a cosmetic that decides who gets tagged.
- **Wearable Accessories**: Bound strictly to headwear/eyewear/jewelry envelopes defined in `docs/wearables_catalog.md` without extending into role-indicating color spaces (`#f87020` offense orange or `#0080e8` defense blue).

---

## 9 · UI Icon Suite & Borderless Carved Wood Palette

### 9.1 The Law of No Blue Outlines
UI icons, badges, rank emblems, and state glyphs must **never** carry dark blue, navy, or cold ink outlines:
- **No Cold Outlines**: Dark blue strokes (`#040838`) on brown wood panels read as harsh blue rings and clash with the game's warm, sunlit street style.
- **Warm Wood & Amber Definition**: All shapes, shields, wings, slippers, and cans are defined by tone-on-tone carved wood geometry (`#31190B` deep wood, `#5A2F14` mid wood, `#8B5227` edge wood), cream parchment inlays (`#F5E6C8`), and glowing amber gold (`#FFBA00`).
- **Escalation**: Ranks escalate in physical frame weight and amber presence from rookie wooden plaque (Bata) to radiant 8-ray sunburst (Alamat).
