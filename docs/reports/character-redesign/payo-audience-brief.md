# Payo audience v2: the brief every villager's agent works from

Written 2026-10-19. The Payo map wants a crowd of precolonial Ifugao and Cordillera villagers on its basin wall.
A first pass (v1, `ArtSource/payo/audience/audience_*.glb`, `tools/author_payo_audience.py`) was six Blender box
figures and the owner REJECTED the construction: "you need to actually model them in the way we've modeled the
rest of the characters. im sure we have documentation on it". The concepts and the dress were good and stay.

The way the rest of the cast is modelled NOW is the redesign pipeline: the nine heroes and the twelve Classic
street people were all rebuilt with it and are the game's models. So each villager is built EXACTLY like a
Classic redesign, with the same rules, the same rig, the same paint and the same clip machinery.

## Read first

1. `docs/reports/character-redesign/classic-brief.md`. ALL of its rules apply (head 0.84, ink eye blocks, one
   thin mouth stroke, blush on female characters only, no nose, no wrinkles at any age, chunky layered hair by
   facing with base/shade/light tones, black is BLACK, the paint rules, the rig with `forearm-left` and
   `forearm-right`, the fist block, cloth across two bones bends, feet on zero, under 6,000 triangles).
   Where this brief differs, this brief wins.
2. `docs/CHARACTER_MODEL_METHOD.md` sections 2 to 4 (tattoo ink is black; markings are hand-set polygons, one fill
   per route, no generator loops; big shapes only) and `docs/CAST_CLOTHING_STYLE.md` (layer edges as geometry,
   one focal fastening, a flared lower layer).
3. Your base: COPY the nearest approved Classic's scripts (author, textures, clips, render, sheet) and rewrite
   them for your villager. Each prompt names the base. The approved Classics' pictures:
   `Logs/character-redesign-classic/v01_lineup.png`, and each one's `Logs/character-redesign-<id>/` folder.
4. The v1 lineup for the concept: `ArtSource/payo/audience/audience_lineup.png`.

## Precolonial Ifugao and Cordillera dress (research it; this is the short of it)

- Men: the bahag (loincloth: a long woven strip, front and back flaps hanging from a wrapped belt), bare chest,
  often a woven blanket or cloak over the shoulders for elders, a head cloth or a rattan/beaded band, sometimes a
  feather; the pinuhha or betel bag (a small woven or rattan bag on a cord); brass or bead ear and neck ornaments;
  a spear (a long hardwood shaft, a leaf-shaped iron blade); bare feet.
- Women: the tapis (a wrapped knee-length skirt of horizontal woven bands, held by a belt), bead necklaces and
  hair beads, hair long and tied or wrapped; a short wrapped top or a blanket over the shoulders is fine (keep it
  modest; this is a game for everyone); bare feet.
- Textiles: Ifugao weave is a dark indigo-black or white ground with thin madder-red and white bands and small
  geometric motifs (diamonds, zigzags, lizard and human figures). Baskets are rattan and bamboo.
- Precolonial tattoos (batok) are black line routes on arms and chest for men who earned them.

## Colour

QUIET. A crowd must never steal a hero's signature hue: Sean red and orange, Cheska light cyan and white, Dante
forest green and gold, Zack yellow, Nemu violet, Phaister magenta, Rafi a muted sea teal with silver, Amihan
dark teal and cream. Keep the role hues `#f87020` and `#0080e8` off everything. So: a dark indigo-black or
undyed cotton ground, red only in THIN bands (a desaturated madder, never a large red area), natural rattan,
brass as the one metal. Skin in the heroes' range.

## Finish: exactly the Classics' (owner, 2026-10-19)

"also compare the textures to the cast, they look noisy. they just dont look like the models we have at all".
The atlas has the base Classic's finish: clean flat tones per form with its base, shade and light steps. No
noise, grain, brush strokes or jitter. Woven bands and motifs are clean hand-set shapes; where the Classics build
a piece as geometry (trim, belts, beads, cords), build it as geometry. Every lineup puts the base Classic right
beside the villager so the finish can be compared.

## Feet, barefoot

Precolonial villagers are barefoot: build bare feet as proper blocks (toes as a slight step is plenty). This
overrides the Classic brief's "proper footwear".

## Kids

A child is built on the same rig and rules, kept at the LOW end of the cast's height range (0.6613 to 0.7928;
aim about 0.67) with child proportions inside it (a slightly bigger head share, shorter legs). Do not scale a
rig below the range.

## Clips (named exactly)

Copy every clip the base Classic has (they come with the rig), then author your own:

- `idle`: 8 s, ACTED, a spectator watching a match: this person's own stances (leaning on the spear, arms folded,
  hand shading the eyes, a nudge to a neighbour). Feet planted.
- `cheer`: a 1 s loop, arms up in a V through the elbows, a small hop or bounce, the head back.
- `clap`: a 1 s loop of applause, hands meeting in front of the chest, a nod into each clap (see
  `author_character_redesign_lola_pacing_clips.py`, her `emote-yes`, for how one is written).
- `sit`: SEATED ON A LEDGE as a spectator, a loop: hips on the ledge top, thighs forward, shins hanging over
  the edge, hands on the knees or lap, a small breathing motion. The ROOT at its rest place means: the seat (the
  underside of the hips) is at z = 0 at the root's origin. Say in your report exactly where the seat is relative
  to the origin.
- Also replace `emote-yes` with the clap and keep `walk`, `sprint`, `jump`, `fall` working (the Classic base's
  versions rewritten lightly for this person are fine).

## Files (your own only; `<id>` is the villager's id)

- `tools/author_payo_audience_v2_<id>.py`, `_textures.py`, `_clips.py`, `tools/render_payo_audience_v2_<id>.py`,
  `tools/sheet_payo_audience_v2_<id>.py`
- `ArtSource/payo/audience/v2/<id>/audience_v2_<id>.glb` (rigged, all clips) and `audience_v2_<id>-atlas.png`
- `ArtSource/payo/audience/v2/<id>/audience_v2_<id>.blend`
- Renders under `ArtSource/payo/audience/v2/<id>/renders/vNN_*.png`

NOT under `Assets/`: nothing in the game loads these yet; the Payo map session will import them.

## Hard limits

- Do NOT run Unity (the owner's editor is open on this folder). Blender headless
  (`C:\Program Files\Blender Foundation\Blender 5.0\blender.exe`) and Python (`py -3`) only.
- Do NOT edit any existing tool, any character's files, anything under `Assets/` or `Resources`, any C#, any map,
  Payo or arena file. Only the files listed above.
- No git commits, pushes or stash. No em dashes anywhere.
- Windows sometimes refuses a write with `OSError` Errno 22: retry. Bash heredocs with apostrophes break and
  compound shell commands may be refused: write scripts to files with the Write tool, run one plain command each.

## Process

Render, LOOK, write down what is wrong, fix, re-render under a new version, at least three rounds. THE TEST every
round: stand the villager in a row with three redesigned heroes (`Assets/TumbangPreso/Art/CharacterRedesign/
<hero>/<hero>-redesign.glb`, e.g. rafi, dante, cheska) AND three approved Classics (bayan, bebang, lola_pacing).
They must belong in that row, read as precolonial Cordillera at a glance, and not copy anyone in it. A crowd
figure is seen small: judge the silhouette and the colour masses at lineup distance first, detail second.

## Deliver

Version; triangles; height; absolute paths of (a) the row with heroes and Classics, front and three-quarter and
back, (b) the face beside two heroes', (c) a turnaround, (d) one still each of `idle`, `cheer`, `clap` and `sit`
at a key frame (the sit on a ledge block); the seat's position relative to the origin; the design choices and
why; the idle stances; an honest list of what is still weak. Stills only.
