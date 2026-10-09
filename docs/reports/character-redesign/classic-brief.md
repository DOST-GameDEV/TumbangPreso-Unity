# Classic cast redesign: the brief every character's agent works from

Written 2026-10-06, after the owner approved the three pattern characters (bayan, bebang, lola_pacing:
"everything looks good"). The agent's own prompt names its character `<id>`, the original model and the
palette file. Everything else is here.

## Context

The nine heroes were redesigned and are the game's models. The twelve Classic street characters are being
REDRAWN AS IF THEY WERE HEROES. One agent a character, in parallel; never touch another character's files.

## Read first

1. `docs/CHARACTER_REDESIGN_DANTE.md` section 15.9 (part D is the Classic rule), then 15 and 13 for the
   heroes' rules and the file layout.
2. THE THREE APPROVED EXAMPLES, which are the pattern. Open their pictures:
   `Logs/character-redesign-bebang/v08_row.png`, `Logs/character-redesign-bayan/v07_row_with_heroes.png` and
   `v07_face_vs_heroes.png`, `Logs/character-redesign-lola_pacing/v07_row.png` and `v07_face.png`. Read their
   scripts and COPY THE ONE NEAREST YOUR CHARACTER as your base: `tools/author_character_redesign_bebang*.py`
   (a girl, on the heroes' body proportions), `tools/author_character_redesign_bayan*.py` (a man),
   `tools/author_character_redesign_lola_pacing*.py` (an elder), with their render and sheet scripts.
3. Who your character is: search the repo for the id and the display name (`docs/CHARACTER_ORIGINS.md`,
   `docs/CAST_CLOTHING_STYLE.md`, the roster's blurbs in the runtime code), and look at the original model
   rendered WITH the character's palette. Classic rigs are shared shapes recoloured by a 16-colour palette per
   person; the examples' render scripts show how a palette is applied in Blender.

## The rule

Redraw this person as if they were one of the nine heroes. Same person (who they are, their age, their
colours, their recognisable pieces), in the heroes' visual language.

- **Head:** `HEAD_SCALE` 0.84, the heroes' rounded head with a flat face plane.
- **Face:** the heroes' simple solid ink eye blocks at the heroes' size, height and ink colour, with one cut
  that gives THIS person's expression (a slant, a half-close, a smile curve). Mouth: one thin stroke at the
  heroes' width. No nose, no wrinkles or realism at any age. Blush on female characters only. Glasses, a
  moustache or a beard only as simple designed shapes, and only if they are truly part of who the person is.
- **Hair:** built and coloured the way the heroes' is: chunky layered locks with real depth, a designed
  silhouette that keeps the character's recognisable cut, a base, a shade and a light tone by facing. Black
  hair is BLACK (the heroes' values).
- **Clothes:** DESIGNED to a hero's richness (clear layered pieces, trim, tone steps, a few considered details
  and small props that fit this person's life and age) while staying an everyday neighbourhood person, not a
  costume. Start from the character's palette colours and give them depth. Never copy a hero's or another
  Classic's outfit. Proper footwear.
- **Body:** the heroes' proportions as the base (see how bebang's script did it); a big or broad character may
  keep extra width as a trait.
- **Paint:** drawings only on faces square to a projection, flat tones on angled faces, paint stops at its own
  piece, no painted hair shine. Role hues `#f87020` and `#0080e8` off large areas.
- **Rig:** the original seven bones, same names and hierarchy, plus `forearm-left` and `forearm-right`. The
  hand is a bare fist block at the far end of the forearm (the last 14 per cent of the arm) with nothing wider
  beyond the wrist; keep the examples' build-time check. Cloth across two bones bends. Test the head turned 55
  degrees and nodded 22.
- **Clips:** new `idle` (8 s, ACTED, stances that belong to this person only), `walk`, `sprint`, `jump`
  (standing, two feet, arms up in a V through the elbow), `fall`; squash and stretch on root; every other clip
  kept.
- Feet on zero, under 6,000 triangles.

## What to make (your own files only)

- `tools/author_character_redesign_<id>.py`, `_<id>_textures.py`, `_<id>_clips.py`,
  `tools/render_character_redesign_<id>.py`, `tools/sheet_character_redesign_<id>.py`
- `Assets/TumbangPreso/Art/CharacterRedesign/<id>/<id>-redesign.glb` and `<id>-redesign-atlas.png`. If the
  owner's open editor has not already written `.meta` files for them, copy Dante's with fresh guids and point
  the glb meta's texture at your atlas.
- `ArtSource/<id>/redesign-20261006/<id>_redesign.blend`
- Renders under `Logs/character-redesign-<id>/vNN_*.png`

## Hard limits

- Do NOT run Unity. Blender headless (`C:\Program Files\Blender Foundation\Blender 5.0\blender.exe`) and
  Python only.
- Do NOT edit the original glb, anything under `Resources`, any C#, any existing tool or another character's
  files, or any map or arena file (the worktree holds another session's uncommitted arena work).
- No git commits, pushes or stash. No em dashes anywhere.
- Windows sometimes refuses a write with `OSError` Errno 22: retry. Bash heredocs with apostrophes break:
  write scripts to files.

## Process

Render, LOOK, write down what is wrong, fix, re-render under a new version, at least three rounds. THE TEST
every round: stand the character in a row with three redesigned heroes AND the three approved Classics'
models (the bayan, bebang and lola_pacing redesign glbs). They must belong in that row and must not look like
a copy of anyone in it. Be critical.

## Deliver

Version; triangles; absolute paths of (a) the row with heroes and approved Classics, front, three-quarter and
back, (b) the face beside two heroes' and the original's, (c) a turnaround, (d) the head-turn test; the design
choices you made and why; the idle stances; and an honest list of what is still weak. Stills only.
