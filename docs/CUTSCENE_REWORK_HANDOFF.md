# Ultimate cutscene reworks: handoff

Rewritten 2026-10-09 by the session that did batch 1 (Dante, Cheska, Sean). The first version of this file (2026-10-08,
by the session that reworked Paete's) was never committed, so what still holds from it is repeated here. Paete's is the
worked example; its full record, with every owner quote, is `docs/CHARACTER_REDESIGN_DANTE.md` section 15.11 (read it to
the end). Read `AGENTS.md`, `docs/HERO_KIT_METHOD.md` and `docs/SKILL_NETWORK_CONTRACT.md` first.

**The continuation prompt is NOT in this file.** A handoff prompt goes in chat, never in a committed file (`CLAUDE.md` 2.4).

**Since 2026-10-11 the effects themselves have their own method document, `docs/VFX_BENCH_METHOD.md`.** Read it before
making or refining any ability or cutscene effect: references first, build from the approved bench effects, lock what
he approves, daylight is not the bench. It also carries the current state of the bench and of Amihan's WHIRLWIND.

## READ THIS FIRST: where things stand on 2026-10-10 (the polish pass)

Everything below this section is the record up to 2026-10-09 and is still true unless this section says otherwise.

### What changed since section 1 was written

- **The stagings are agreed enough to polish.** The owner: "continue polishing all the ultimates. i dont want to see anything
  bare in textures, anything looking unpolished, no vfx etc." So the "do not polish blocking" rule in section 5 is over.
- **Another session COMMITTED this worktree** (three commits on 2026-10-09, the first `bd44c197b` at 21:13). The cutscene work
  up to then is in git on `QoLUpdates`. This session has still committed nothing and must not until he asks. Work after
  21:13 is uncommitted again.
- **Sean:** 8.4 s now (was 7.0). "sean should have more airtime before going to the fpv. it should also tween to the back of
  sean in a tpv type of way before going to fpv": same leap, slower over the top, he hangs; the last shot swings round
  behind him 7.15 to 7.85 (`SnBehind`), holds; his own screen goes into his eyes from 8.02 (`SnHome`). Then "sean jumps up
  and clips his head on the magic star effect . the effect should look like its reaching from the sky and large": the
  lantern is 16 m up and 24 m across (`SnHigh`, `SnGiant`). The teleport's cleanse on his ultimate STAYS ("keep"), and the
  same will hold for Dante.
- **Sean's live landing** lost its two pixel-art sprite sheets ("i dont like the pixel impact effects, doesnt fit our
  style"; `HeroHazards.CreateExplosion`, the default/Fire style). He played it: "looks good". The other styles (Dante's dust,
  Cheska's frost, the slipper's smoke, Ignition, Zack's, Nemu's) still play their sheets; he was asked whether those should
  go and has not answered.
- **Rafi** was restaged to his own idea joined to the tide scene, 8.6 s; see "Rafi" below.
- **Dante's wind-up** after the cutscene: "we'll see this later once refined and in game". His live "really up there" part
  is still NOT built.
- **Paete is invisible in the prototype map** (`Scenes/Temp/CharacterPrototype.unity`): the scene was saved 2026-10-07 and
  his model was re-exported with vine bones on 2026-10-08, so the scene's copy has the old bone list. Not this work. The
  fix is the menu item Tumbang Preso > Character Redesign > Build Prototype Map (temp scene). He was told; not confirmed.

### The style law for anything modelled (he corrected this twice in one day; do not repeat it)

1. Of a shark built as flat palette-slot prisms with a hinged jaw: "shark model is not at all good, jaw is messed up,
   doesn't fit the hand painted illustrated stylistic art design of our characters and overall game style. it's also tweened
   so poorly that it doesnt look like its smoothly jumping out the water and diving down.. need you to read up on how we
   modeled the fpv hand pets (currently hidden) and the character model redesign docs".
   **So an object is built with `tools/hand_companion_kit.py`** (round forms, smooth normals, a PAINTED atlas beside the .glb
   through `Model.texture` and `grid_uv`), never with the prism builder `tools/build_paete_props.py`. Read
   `docs/CHARACTER_REDESIGN_DANTE.md` 15.3, 15.9 and 15.11 ("How a plant is now built") and a real builder
   (`tools/build_paete_bloom.py`, `build_paete_sentry.py`, `build_hand_<hero>.py`). **Look at it in Blender first**
   (`tools/review_hand_companion.py`, then `py -3 tools/hand_companion_kit.py sheet <name> <tag>`; Blender 5.0 is at
   `C:\Program Files\Blender Foundation\Blender 5.0\blender.exe`; pictures land in `Logs/hand-companions/`).
2. Of a round cute painted shark made of the kit's balls and leaves: "shark could probably be less bubbly, also maybe it
   should be a whale shark instead", "it should be more fierce looking, and preferably not made with basic object shapes
   just put together without proper connection". **So a creature is ONE SKIN**: `tools/build_ultimate_shark.py` fuses typed
   forms in Blender (voxel remesh, smooth, decimate), lays UVs round the long axis and paints the atlas with PIL.
3. Of a fused whale shark built from memory: "that's a really bad whale shark model. i need you to research what a whaleshark
   looks like and give me references for you to follow and analyze". **So look at the real thing before modelling it.** The
   references and their sources are in `docs/art_refs/whale-shark/` (A side, B head-on, C three-quarter from above, D from
   below, E to G mouth open; Wikimedia Commons, each credited in section "Whale shark references" below).
4. **Motion:** nothing with weight moves on a straight lerp. Arcs, anticipation, ease, overshoot, settle, facing along the path.
5. **Effects** (after the first polish films): "you should be more creative with particle effects and do more with
   emmissives." Designed particles (shaped, layered, timed, in the hero's own language) and real emissive light.
6. Water: "water doesnt have a good water shader, nor does it have any flowing water and wave effects, maybe take a look at
   lagooncove again" (`Assets/TumbangPreso/Shaders/LagoonCoveWater.shader`). ⚠️ That shader runs on `_Time` and scene depth;
   a cutscene runs with the match paused and the film runs outside Play, so a cutscene's shader needs its own clock, set
   from the scene clock every frame.

These are also in memory as `props-painted-kit-not-prisms`.

### How the polish is being done: parallel sessions, one lock

He asked for it: "do the rest in parallel", then "send this to subagents, work on rafi through a subagent too", with the main
session keeping the documentation.

- Each hero is one background agent that owns that hero's files only: `HeroIntroductionScene.<Hero>.cs`,
  `Runtime/Visual/UltimateProp.<Hero>.cs` (a palette, `public static partial class UltimateProp`),
  `tools/build_ultimate_props_<hero>.py`, its models under `Resources/Models/UltimateProps/`. `UltimateProp.cs` is the shared
  loader (`Spawn`, `SpawnSpirit` for a ghost drawn by `Shaders/SpiritGhost` as Makiling is, `Presence`).
- **`tools/cutscene_rework/agent_cycle.sh <hero> <tag> <draft folder>`** is the only way anything is saved or filmed: it
  compiles the whole project outside Unity with the draft folder's .cs files standing in, and only if clean takes one lock
  (`Temp/cutscene-film.lock`), copies the files in, asks the editor for the film and waits. So no session's save recompiles
  the editor in the middle of another's film. ⚠️ It has once printed `Permission denied` on the copy and then filmed stale
  code: after a cycle, `cmp` the draft against the project file. ⚠️ A compile can fail on ANOTHER session's half-saved pair of
  files; retry.
- An agent's Write and Edit tools refuse paths in this worktree, so it edits a draft folder in its own scratch directory.
- `tools/cutscene_rework/cc_draft.sh` now also picks up files committed since its old rsp snapshot was made.
- **Own pass after every agent** (his standing rule): look at each agent's contact sheet yourself before showing him.
- It costs a lot: the first round of five agents used about two million tokens.

### Where each hero stands

Films and sheets are in `Logs/paete-ability-film/`; a sheet is `sheet_<hero>_<tag>.jpg`.

| Hero | Length | First polish film | His verdict | Round two (running when this was written) |
|---|---|---|---|---|
| Nemu | 7.2 s | `nemu_p4` | "the ghost needs a remodel. needs more vfx/particles" | Remodel the giant (rage) Kuro in the painted one-skin style through a tool like `tools/kubo_kuro.py` (same node names, rig, clip: it is the LIVE monster too), and more designed VFX |
| Sean | 8.4 s | `sean_p3` | "sean's parol looks good, but the ground effects/magic circles are lacking proper shaders and effects" | A new ground shader and emissive, living landing mark; light cast on the court; designed embers and sparks |
| Zack | 6.4 s | `zack_p3` | none yet | none |
| Dante | 7.0 s | `dante_p4` | "the longhorn lacks texture. the actual in-game ulitmate is just effects on the ground, it doesnt sell that weight of the longhorn's slampunch" | A: paint the longhorn properly, emissive seams. B: the LIVE ultimate's look (`DanteDriftVisual`, look only, nothing that decides gameplay): the longhorn's fist coming down on each band |
| Cheska | 11.9 s | `cheska_p4` (sheets p3 and p4) | none yet | none |
| Rafi | 9.2 s (was 8.6, for the dive) | `rafi_q4` | Second agent pass, awaiting his verdict: whale shark rebuilt from the references (`Logs/hand-companions/shark_w7_vs_refs.jpg`), bending spine (`RafiSharkSpine.cs`), new `RafiSea.shader` with rounded two-layer caustics after his two reference images, the wall as a thick mass he dives into, props rebuilt in the painted kit | Wall reads as a dome from the ground shot; crash crown reads icy; entry ring busy for 3 frames; swim bend unjudged in motion; live water field (`RafiWaterVisual.cs`) untouched |
| Amihan, Phaister | unchanged | not started | | |

What the first round built, by the agents' own reports (each checked on its sheet by the main session):
- **Nemu** (`HeroIntroductionScene.Nemu.cs` only): an unlit ink pool rewritten each frame with a living edge and halftone dots,
  drawn with Kuro's tail shader; ink curtains for walls; sixteen real eyes that open, blink and track; a splash crown on the
  dive; ink tearing off him on the rise; teeth at the frame edge before black. Vertex colours are stored linear.
- **Sean**: models `parol`, `parol_small`, `duskline` (a quarter town stood four times) on one painted atlas; the lantern is
  built stick by stick, then panels; bulbs on cords; a shaped fire trail; a filled star landing mark; the burst throws the
  lantern's own panels. `SeanParolAt` now arcs and overshoots `SnHigh` by about 0.55 m, settling by 4.95 s.
- **Zack**: eight painted models (`zack_condo`, `office`, `mast`, `flats`, `row`, `cloud_a`, `cloud_b`, `cloud_top`) on
  `zack_storm-atlas.png`; a typed bolt with core, sheath and forks on a new `ZackTip` motion; fat hung rain with frozen
  splash rings; a dome and a dial under each player for the stop; scorch plates and thick arcs after the hit. The backdrop
  material draws at queue 2900.
- **Dante**: `longhorn.glb` with a painted atlas (head and jaw are joints, the right arm still its own node); broken plate
  edges; a gradient lava sea with veins; shards, dust, embers; crags and smoke on the horizon. Almost everything is opaque and
  grows or shrinks, because see-through pieces sorted badly against the dust walls.
- **Cheska**: `iceblock`, `icecube`, `glacier`, `icespike_a/b/c`, `icechunks`, `frostpine` with `ice-atlas.png` and
  `iceblock-atlas.png`; each frozen player also gets the live game's own restraint shards inside the block; a sky, ridges and
  pines for her world from the glacier on. Its eight `.glb.meta` files were hand-written from `shark.glb.meta` with fresh
  guids and still name `Textures\colormap.png` as a dependency: worth a look.

Weak points the agents named and nobody has fixed: Dante's stomp lands at the frame's bottom-left edge with 4 frames left (a
shot or timing change); Cheska's backdrop only exists from the glacier on, her burst is 0.3 s and small, and her ice block
still vanishes at the hand-back because the live Frozen status draws no block; Zack's city is busy and bright; Sean's sky
over the town is a flat brown wall. None of the five has been seen in a match, on a real map, with reduced effects, or with
a held slipper.

### Rafi (the one with the most history)

- His idea, joined to the tide scene and agreed ("rafi's idea sounds good"): the tide runs off the court and stacks up behind
  him; he dives into the wall; under water he rides a shark; they burst out, he lets go and lands; then "rafi getting off the
  shark should be a from-ground camera and the shark should end up becoming ghostlike as it enlargens to the sky, like
  paete's ult makiling. and then the shark drops in front of rafi. camera position stays there, meaning its all in one
  shot." The hit makes the wave; a loose slipper rides it to his feet.
- Built by the main session, films r3 to r10: the one ground shot (`_rfGround`, index 4, 5.0 to 7.6); the ghost by
  `UltimateProp.SpawnSpirit`; the flight as one Hermite curve (`RafiSharkPath`) with the nose following the path; a darker
  body behind the wall's face and fish swimming in it; faceted water drops; the under-water set as a closed blue shell 60 m
  OVER the court (`RfDeep`), never seen on a real map.
- **The shark has been refused three times** (see the style law). The research he asked for is done and shown to him. Asked
  how to be fierce he chose **"do #1": fierce through attitude, true face**: the real head, tiny eyes, a wide straight
  mouth held open and dark, a hard downward tilt in the dive, darker paint round the eye; no sculpted brow.
  What a whale shark is, measured off reference A as shares of length: the head a wide flat spade about twice as wide as
  tall and the widest part of it; the mouth a straight slot across the very front, nearly the head's full width; tiny eyes
  low behind the mouth's corners; deepest just behind the head (17%), tapering to a thin stalk (3%); three ridges a side,
  the lowest a keel on the stalk; the first back fin at 51% to 66%, about 10% tall, a small second at 75% to 82%; side fins
  broad wings 21% long from about 20%; the tail a tall crescent 30% tip to tip, upper lobe longer; five gill slits; blue-grey
  to grey-brown over white from the lower lip; small dense spots on the head, larger spots between pale bars on the body.
  The refused one in the game now (`shark.glb` as built by the current `tools/build_ultimate_shark.py`, picture
  `Logs/hand-companions/shark_w2.png`) has a cone head, a small frown, big browed eyes, the back fin too far forward.
- **Still open, all handed to Rafi's agent:** the shark; the water (shader, flow, waves); the dive ("the wave is so thin so
  it looks like he's diving into basically not much water, then it cuts mid-dive to him being underwater"); and the boat,
  fish, stilt houses and slipper in `tools/build_ultimate_props.py`, which are still the first faceted kind. That agent may
  change Rafi's shots, keys and length in `rafi()` of `tools/author_ultimate_intros.py`; if the length changes, update the
  protocol 156 comment in `NetSession.cs`, which records every length.

### Whale shark references (`docs/art_refs/whale-shark/`)

| File | Source |
|---|---|
| `A_side_profile.jpg` | NOAA FGBNMS/Eckert, public domain: commons.wikimedia.org/wiki/File:Rhincodon_typus_fgbnms.jpg |
| `B_head_on.jpg` | Derek Keats, CC BY 2.0: commons.wikimedia.org/wiki/File:Whale_shark,_Rhincodon_typus,_at_Daedalus_in_the_Egyptian_Red_Sea._(35827412321).jpg |
| `C_three_quarter_above.jpg` | Matemi01, CC BY 4.0: commons.wikimedia.org/wiki/File:Whale_Shark_(Rhincodon_typus_A._Smith,_1828).jpg |
| `D_from_below.jpg` | Zac Wolf, CC BY-SA 2.5: commons.wikimedia.org/wiki/File:Male_whale_shark_at_Georgia_Aquarium.jpg |
| `E_mouth_open.jpg` | Matthew T Rader, CC BY-SA 4.0: commons.wikimedia.org/wiki/File:Whale_Shark_(Rhincodon_typus)_with_open_mouth_in_La_Paz,_Mexico.jpg |
| `F_mouth_open_below.jpg` | Arturo de Frias Marques, CC BY-SA 4.0: commons.wikimedia.org/wiki/File:Whale_Shark_AdF.jpg |
| `G_mouth_open_front.jpg` | Don McCulley, CC BY-SA 4.0: commons.wikimedia.org/wiki/File:Rhincodon_typus,_whale_shark_IMG_7075.jpg |

These are study references. They are other people's photographs under their own licences and must not ship in the game.

### What to do when a round-two agent reports

1. Read its report. Look at its last sheet yourself; for a model, its Blender review picture too.
2. `cmp` its draft against the project file if anything looks stale.
3. Send him the film (1x then 0.5x) with what changed and what is still weak. He answers in a line; pass it on to the same
   agent (it keeps its context) or do it yourself.
4. Keep this section's table current.

## 0. Where the work is (this cost the last session its first hour)

- It is all in the worktree `.claude/worktrees/kanto-blender-assembly-909442`, branch `QoLUpdates` (uncommitted when this
  was written; committed by another session on 2026-10-09, see the top). A
  session opened in any other worktree (for example `heroes-ultimate-cutscenes-rework-8b26f3`, which is 2,000 commits
  behind) will not find these docs or this code. Edit the files in the kanto folder directly. The owner: "yes work
  directly there, and then we'll be pushing changes to the qolupdates branch occasionally."
- ⚠️ From a session in another worktree the Write and Edit tools REFUSE paths in the kanto folder (a hook). The last session
  wrote every file to its scratch folder and copied it across with Bash, which the owner had agreed to. Starting the session
  IN the kanto folder avoids this.
- Do not commit or push until he asks. When he does, ask what to include; never include his own files (listed in section
  15.11's prompt).
- ⚠️ THE EDITOR IN PLAY MODE TAKES NO FILM. On 2026-10-09 it sat in Play for an hour while he was away (`Logs/Editor.log`
  ends on "Entering Playmode" and stops growing; another session had also just built the Eskinita map in it). Saves still
  go in. Do not stop Play for him; say so and ask.
- His Unity editor is normally open on that worktree and he plays in it. **At the end of the last session it was CLOSED**
  (no `Temp/` folder, no `Unity.exe`). Never start or kill Unity. With it closed nothing can be filmed: say so and ask him
  to open it.

## 1. The job, and where each hero stands

The owner, of Paete's cutscene after its models were only repainted: "this is essentially just the same cutscene textured
better ... it could use some more creativity on it". Every other hero gets a bolder staging idea chosen from options, real
weight in the timing, sound picked by ear, and every detail checked from where the camera really stands.

**Batch order, confirmed by him:** 1 Dante, Cheska, Sean. 2 Zack, Nemu, Rafi. 3 Amihan, Phaister, and for batch 3 he wants
"new staging idea", not only polish.

| Hero | Length | State | Last film |
|---|---|---|---|
| Dante | 7.0 s (was 3.8) | Blocking cut, staging accepted so far. Live half NOT built | `introfx_dante_d5.mp4` |
| Cheska | 11.9 s (was 3.2) | Seventh staging; the heart is the part he worked on most. The arm now SHOWS (measured pass done); he has not seen it | `introfx_cheska_c34.mp4` |
| Sean | 7.0 s (was 3.4) | Density pass and the leap FILMED (no error; he has not seen it). The hand-back camera and the live start at the apex have never been PLAYED | `introfx_sean_s7.mp4` |
| Nemu | 7.2 s (was 3.8) | New staging, UNPITCHED, filmed three times, he has not seen it | `introfx_nemu_n3.mp4` |
| Zack | 6.4 s (was 2.8) | New staging, UNPITCHED, filmed three times, he has not seen it | `introfx_zack_z3.mp4` |
| Rafi | 8.6 s (was 3.4) | HIS idea joined to the tide scene (the tide goes out, he dives in, rides a shark under water, they burst out, he lands, the shark hangs enormous and crashes down, and the crash is the wave). Blocking, filmed twice | `introfx_rafi_r4.mp4` |
| Amihan, Phaister | unchanged | Not started | baseline not filmed |

Films are in `Logs/paete-ability-film/`. Every cut is BLOCKING: stock shapes, no sound, no real map, film stand-ins for the
other players. Nothing in this rework has been seen in a match, on a real map, with the game's grade, or heard, by anyone.

### 1.1 Dante, CONTINENTAL DRIFT

Chosen: "dante i like 2 and 3", joined, "About 7 s". `HeroIntroductionScene.Dante.cs`, table `_dante_drift`.

- 0 to 1.15 he punches the court. 1.15 to 3.4, from above, the court is seven plates adrift on a molten sea. 3.4 to 5.3
  the horned basalt back rises under his plate and lifts him 3.4 m. 5.3 to 7.0 it raises a fist as he loads both of his;
  they come down as one at 6.88 and the cutscene ends there.
- "end on the fist landing", then: "the cutscene should end exactly when the fist lands before switching back to live
  first person gameplay, by then dante should still be on the longhorn and only then will it disappeaer into the ground".
  So nothing sinks and no blast is shown in the cutscene, and his stage does not fade at its end.
- ⚠️ NOT BUILT, and it is gameplay and network. Asked whether the going-under is look only: **"he's really up there"**. Asked
  whether he is held on it while it sinks: **"he can"** walk off. So: at the hand-back the host puts him 3.4 m up on a real
  thing he stands on, on every peer; it sinks over about 0.8 s; he may step off and fall; the five blasts still start from
  the court; he can be hit at that height. It rides on protocol 156. Until it exists the hand-back pops (the titan
  vanishes and his live body is on the ground). Sean's `IntroductionIsTheWindup` and teleport (1.3) are the nearest pattern.
- Not started on 2026-10-09 either, on purpose: with the owner away it would have been gameplay written unseen, and it
  needs two answers from him first. (1) Does CONTINENTAL DRIFT lose its 0.4 s wind-up after its cutscene, as Cheska's and
  Sean's did (`IntroductionIsTheWindup`)? The cutscene ends on the stomp, so a wind-up after it is a pause with him standing
  on the titan. (2) Putting him up there is a `Teleport`, which clears his statuses, the same open question as Sean's.
  `Earthquake.OnActivate` (`DanteHeroKit`) runs on every peer and already spawns `DanteDriftWave` there; a sinking thing
  with a collider would be spawned beside it.
- The titan is placeholder blocks. Once the staging is agreed it wants modelling as a prop (the owner on Paete's props:
  "its js blocks"). The old cutscene is kept in the table script as `_dante_fissure`.

### 1.2 Cheska, ABSOLUTE ZERO

Seven stagings in one day. The history is in the header of `HeroIntroductionScene.Cheska.cs`; the refused ones are kept in
`tools/cutscene_rework/old/`. The tone that stuck is his: **"cheeky but surprisingly strong"**. What it is now (11.9 s):

| Time | What |
|---|---|
| 0 to 2.8 | she skates one slow loop on a ribbon of ice and turns to a stop |
| 2.8, 4.1, 5.0 | through her eyes, three looks (1.3, 0.9, 0.8 s): the view turns to another player where they really stand, zooms in, she points, and the frame goes to a flat colour with a two-colour star burst and a block of ice slammed down on them |
| 5.8 to 9.2 | from straight above, she skates a one-line heart (`CkRoute`) |
| 9.2 to 9.85 | wide and low: one foot down and a glacier goes up |
| 9.85 to 11.2 | close: she blows the frost off her fingertip, unhurried |
| 11.2 to 11.9 | high and wide: her hands snap apart, the glacier bursts, everyone is left in their block of ice |

His words that shaped it, so nobody undoes them:
- The heart: "wayy too small, should be larger like 2-3x large"; "follow the route i gave, and make the heart more natural -
  the loop looks like an exact circle"; "now it looks like a child drew it"; "no not symmetric at all"; two reference
  pictures (a single-line heart on a waving stroke; a brush heart with one lobe larger), "something like these"; "too
  linear.. its not flowwy at all.. speed should slow down and vary, especially on short loops or turns"; "turn on the small
  heart loop is still too sharp/fast"; "you can remove the tail curl at the end"; "a tad bit too slow for someone skating".
  So it is rows of place and velocity joined by smooth curves, asymmetric, her speed set by how tightly the line bends
  (`_ckRouteClock`), 3.4 s. The line ENDS where she stands (play resumes there), so the heart is drawn to her left.
- Length: "ykw we can extend the ult cutscene until everything looks smooth". `UltimatePerformance.MaxSeconds` is 13.0.
- The blow: "the finger blow at the end needs to end more smoothly meaning its ending too fast" (it has 1.35 s).
- **Rule change, his pick:** the cutscene counts as her 1.5 s delay, so released after its cutscene the freeze lands as play
  resumes ("post-cutscene the players should be frozen"; "i pick 1"). `HeroAbility.IntroductionIsTheWindup`, overridden in
  `CheskaHeroKit`. The frost and ice blocks do not fade at the end of the cutscene for the same reason.
- Fingers: "if we're going to snap, she needs a fingers on her model somehow", then of a hand built with fingers: "the
  fingers are weird, i was thinking just use the original fpv hand, instead make her whole elbow bent and then extend to
  point towards the person". No snap, no fingers; her model is untouched (no thumbs or fingers on any body is a standing
  rule, `AGENTS.md` 2026-09-14).

⚠️ BROKEN: **her arm does not show in the three looks.** Her first-person arm mesh (`Models/RosterArms/cheska_*`) is one
rigid piece; he: "doesnt cheska's model have elbows?" It does (`forearm-left`, `forearm-right`). The current code holds a
second copy of her whole body to the frame with its head scaled to nothing and poses its upper arm and forearm
(`SampleCheskaArm`). Three blind tries: a sliver in the corner (c15), her arm and chest filling the screen (c16), nothing
visible (c17 onward). What to do next: film that arm ONCE from the side with no zoom, measure where shoulder, elbow and fist
are, and set `seat`, the scale and the `upper` directions from those numbers. He has not answered whether to do that pass.
The rigid arm that at least showed is film c13.

**The measured pass was run (2026-10-09) and the arm shows** (`introfx_cheska_c34.mp4`, `sheet_cheska_c34.jpg`). What the
numbers said, in order: the shoulder was 0.13 to 0.25 m BEHIND the lens and her upper arm is only 0.18 m long, so all of it
stayed behind (c29). Seated in front, the arm came up and her chest came with it and filled the picture (c30): her chest
is wider than her arm is long, so no seat shows one without the other. So the copy now keeps ONLY the arm (the bone it
hangs from is shrunk to a thousandth and the arm grown back by the same; every other limb shrunk away; her model is not
touched), c31. Then it was too near and grew on each tighter look, because the seat came nearer with the zoom while the
copy also shrank with it; it is now a fixed 0.75 m off (c34). It is her real, short arm with its fist, low on her side of
the frame. Not done: it does not bend then extend as visibly as he asked (the bend happens mostly under the frame edge),
and it has not been judged by him. The log stays for the next adjustment:

**How the measuring works.** `HeroIntroductionScene.CheskaArmLog`: any film of her
(`introfx cheska <tag>`) now also writes `Logs/paete-ability-film/arm_cheska_<tag>.txt`, one block per frame the arm is up:
where the shoulder, the elbow, whatever hangs under the forearm and the middle of every surface of the copy are in the lens's
own space (right, up, forward, metres) and where that falls on the frame (-1 to 1 each way, or BEHIND THE LENS), with each
surface's enabled, active and forced-off state. Read that file first: it says whether the arm is off the frame, behind the
lens, the wrong size, or simply not being drawn, and the fix to `seat`, the scale or `upper` follows from the numbers.

Other open points on her: the stomp and glacier are 0.65 s, the shortest beat in a 12 s cutscene; her body on the skates
is two alternating poses and a lean; the colour panels and the hand are stood for the AUTHORED lens (see 3, ChooseShot);
the live Frozen status draws its own ice prison, never compared with the cutscene's blocks; `-held` unfilmed.

### 1.3 Sean, SUPERNOVA

Chosen: "sean i like 1 and 2", joined, "About 7 s". `HeroIntroductionScene.Sean.cs`, table `_sean`.

- 0 to 2.2 he builds the parol as before. 2.2 to 2.9 he lifts it and lets it go. 2.9 to 4.9 the lens follows it up as it
  grows into the giant lantern (6.2 m up, 11 m across) and lies over above the court. 4.9 to 6.0 from above: the landing
  ring and who stands in it. 6.0 to 7.0 the coil, then he LEAPS at 6.2, the lantern bursts round him at 6.72, and he is at
  the top at 7.0.
- Of the first cut: "it looks really basic". The density pass answers it: layers under the parol, five rings of lights
  running in waves then chasing, a fire trail and shed sparks on the climb, an ignition (a ring across the sky, twelve rays,
  a rain of sparks, shafts of light on the court), a grade that dims the world.
- "he should leap but then at the apex of the leap, the person playing sean should have their cam tween to their fpv. and
  the rest of the players cut to their fpv." Built as:
  - `HeroIntroductionScene.HandBackView` (new, called by `UltimatePhaseView.Draw` only on the screen watching the caster):
    from 6.52 the lens travels to where his first-person camera will be (the live camera's pose raised by the apex).
  - **Rule change:** released after its cutscene SUPERNOVA has no wind-up and no launch. `SeanHeroKit`: he is put
    `SupernovaApexRise` up (4.675 m, or as high as the roof allows) by `CharacterMotor.Teleport`, hangs 0.14 s and dives. A
    cast with no cutscene launches as before.
- ⚠️ ALL OF THE ABOVE COMPILES AND HAS NEVER RUN. To check the moment his editor is open: `introfx sean <tag>` (does it
  throw; does the leap read; the burst). And only he can judge, in Play: the camera arriving in his eyes with no jump, the
  dive and the slam after a teleport into the air (there is a guard so the slam cannot fire while he is still up, and it is
  a guess), under a low roof (the cutscene still shows 4.7 m).
- ⚠️ `Teleport` CLEARS HIS STATUSES, as every teleport does. So his ultimate now also cleanses him. He was told and has not
  answered. If he does not want it, he must be placed another way.

### 1.35 Owner's notes of 2026-10-09 on Sean s7 and Rafi r4, both built (films `introfx_sean_s8.mp4`, `introfx_rafi_r5.mp4`)

- Sean: "sean should have more airtime before going to the fpv. it should also tween to the back of sean in a tpv type of way before going to fpv". Now 8.4 s: the same leap, slower over its top, he hangs; the last shot swings round behind him 7.15 to 7.85 (`SeanFrame`, `SnBehind`) and holds; his own screen goes into his eyes from 8.02 (`SnHome`). The burst is at 7.02. By the author's eye the burst's star lines cross him heavily in the behind view. The move into his eyes is still unseen (the film does not run `HandBackView`).
- Rafi: "rafi getting off the shark should be a from-ground camera and the shark should end up becoming ghostlike as it enlargens to the sky, like paete's ult makiling. and then the shark drops in front of rafi. camera position stays there, meaning its all in one shot." Now one shot 5.0 to 7.6 from a fixed low lens (`_rfGround`, kept off the others once at build), which only turns; the shark has a twin made of light on the same joints and becomes it as it outgrows him. In the film two stand-ins stand between that lens and him; in a match the others stand anywhere, so this shot may need a clearer side chosen for it.

### 1.4 Batch 2, written on 2026-10-09 (Nemu, Zack, Rafi)

**Rafi was restaged again the same day, to the owner's idea** (his words are in the header of `HeroIntroductionScene.Rafi.cs`; "rafi's idea sounds good" to the joined version). The Rafi paragraph below describes the version before it, kept as `tools/cutscene_rework/old/HeroIntroductionScene.Rafi.second-staging.cs.txt`. The shark is blocks, agreed. The underwater set is a closed blue shell 60 m over the court, not under it: never seen on a real map. Also decided that day: **the teleport's cleanse on SUPERNOVA stays** ("keep"), so Sean's ultimate clears his non-stun statuses, and the same will hold for Dante. Dante's 0.4 s wind-up: "we'll see this later once refined and in game".

**Update, same day:** the editor left Play at 08:03 and all three were filmed and corrected from their contact sheets
(`Logs/paete-ability-film/sheet_<hero>_<tag>.jpg`). None threw. What the films changed: Nemu's floor eyes were dots (now
2 m pairs) and her ending was the purple inside of Kuro's throat (the black shell is now a hand's breadth from the lens);
Zack's close shot held only his hair and his hand cannot reach above his ear (the shot is wider, the bolt is three times
as thick, and its tip now waits beside his head where his fist is); Rafi's sand was drawn over the leaving water (sorting
order set), and his fish and slipper were specks (bigger, the slipper twice life size as blocking). Still weak by my own
eye: Rafi's stacked wall reads as a pale hill, not four storeys of sea; Zack's rain is faint from the wide shot; Nemu's ink
floor is grey on the film's pale ground. The owner has seen none of it. The paragraphs below are as first written.

The owner, leaving for an hour: "work on something for the rest of the heroes, then refine this first batch of 3. surprise me
with something really good." So these three were NOT pitched and picked the way batch 1 was. His editor was in Play mode
for that whole hour, and the film only runs when it is not playing, so **none of the three has been seen by anyone, the
author included.** Each compiles outside Unity and is saved. Each could throw the first time it is built. Film each
(`introfx nemu n1`, `introfx zack z1`, `introfx rafi r1`) before anything else, fix what breaks, then show him and ask
whether each IDEA stays before polishing any of it. The first cutscenes are whole in `tools/cutscene_rework/old/`
(`HeroIntroductionScene.<Hero>.first-staging.cs.txt`); their tables are in git.

- **Nemu, LIGHTS OUT (7.2 s).** Her look at the lens is kept (0 to 2.45). Then from high over the court the ink runs across
  the whole floor, little Kuro hops and dives into it, and he comes up out of it behind her far too big (2.4 times his
  devouring size). His head turns to each other player where they stand and a pair of eyes opens in the floor under each.
  Low and close: her small and calm, him the whole sky. She points, he comes over her head at the lens mouth first and the
  picture is swallowed; it ends BLACK and does not fade. Unknowns: his real size and where his feet are at that scale, whether
  his mouth really reaches the lens (`_rage.MawPosition`), the black (an unlit shell round the lens for the film; in a match
  also `NemuGrade` to zero, which the film does not apply).
- **Zack, THE BOLT WAITS FOR HIM (6.4 s).** The call is kept. The real bolt comes down the sky and the world stops with its
  tip an arm's length over his hand: the rain hangs, everyone else is a statue, the colour drains (`ZackGrade`, match only).
  He looks at the lens, reaches up and pokes the lightning, shrugs at it in a shot that travels round the stopped court, then
  lets the world go: the bolt lands, the ring goes out to the ultimate's 4.5 m, whoever stands inside jumps, and he is left
  crackling. One clock (`ZackWorld`) stops the rain, clouds and flicker together. Unknowns: whether his raised hand is
  anywhere near the hung tip (`ZkHung` is a guess), the poke and shrug poses, the bolt's size.
- **Rafi, THE TIDE GOES OUT FIRST (6.0 s).** The feints and beckon are kept. From high: on his pull the ankle-deep sea runs
  off the whole court toward him, leaving everyone on wet sand with fish flopping. Low: all of it is stacked behind him four
  storeys high with a little boat on top, and he shrugs. He sends it; it falls to the LOW front the ultimate really is,
  crosses the court carrying each player a step, and a loose slipper rides it to his feet ("Leaves with his slipper").
  Unknowns: everything about how the water pieces read from above, the boat and slipper are blocks, the last pose.

All three stage the real other players (`StageOthers`). All three lengths are in the protocol 156 comment. Tests that may
now be stale: any that pin 3.8, 2.8 or 3.4 s, and Nemu's framing test for the old fitted shot (the fit is gone).

## 2. Tools made this session (in the repo now, so they survive)

`tools/cutscene_rework/`, all run from the kanto worktree root with Git Bash:

- **`cycle.sh <hero> <tag> <edit.py or -> <runtime .cs basenames...>`**: copies those files to a draft folder, runs the edit
  script on the drafts and on `tools/author_ultimate_intros.py`, COMPILES OUTSIDE UNITY, and only if that is clean:
  regenerates the tables, restores every other hero's table from `intros_backup/`, saves the drafts over the project
  files in one go, writes the film request, waits for it, and joins the video. A failed compile saves nothing. This is how
  "compile outside Unity before every save, keep saves few" is kept. An edit script takes `<draft dir> <table script path>`
  and patches by exact string with an assert on the count, so a stale patch fails loudly.
- **`save.sh`**: the same arguments and steps as `cycle.sh` but it stops after the save and asks for no film. For when the
  editor is in Play (a film request then just sits there) or when several heroes are saved before one filming pass.
- ⚠️ ONE FILM QUEUE AT A TIME. There is one request file. Two waiting scripts overwrite each other's request, and stopping a
  background shell from the session does not always stop its loop: check `ps -ef | grep film` and kill it.
- **`cc_draft.sh <Assembly> <draft dir>`**: the compile itself. ⚠️ It reuses the rsp and reference dlls of an OLDER session's
  scratch folder (`O=` at its top) which `cc_rt.sh` and `cc_ed.sh` in the system temp folder also write to. If that folder
  is gone, `cc_draft.sh` cannot run: rebuild the rsp from `Library/Bee/artifacts/1900b0aE.dag/<Assembly>.rsp` the way
  `cc_int.sh` did. `RUNTIME_REF=` points an editor draft at a runtime draft's ref dll.
- **`video.py <hero_tag>`**: frames to mp4, once at 1x then once at 0.5x. He asked for exactly that ("i just need a 1x and
  .5x version"). Do not use `tools/video_paete_ability.py` for these.
- **`sheet.py <hero_tag> <out> <frame numbers...>`**: a contact sheet with times on it. Look at this yourself before sending.
- **`intros_backup/`**: the tables as they should be. ⚠️ After changing a hero's table on purpose, copy its new
  `Resources/UltimateIntros/<hero>*.txt` in here, or the next cycle for another hero puts the old one back.
- **`old/`**: the refused Cheska stagings and Sean's first, whole files.

The film: request `introfx <hero> <tag>` in `Temp/paete-ability-film.request` (`Editor/MapKit/PaeteAbilityFilm.Intro.cs`,
generalised this session; `introfx <tag>` alone is still Paete). Output `Logs/paete-ability-film/introfx_<hero>_<tag>_frames/`.
For Nemu it also stands Kuro beside her (`HeroIntroductionScene.FilmCompanion`, added 2026-10-09: her stage copies him off
her live body and the film's caster had none, so her cutscene could never be filmed before; that path has not run yet).
For any hero but Paete it stands three of the cast and a can position on the court as stand-ins
(`HeroIntroductionScene.FilmStandIns`, `FilmCan`): they are T-posed, which a real match's copies are not. It clears
`UltimatePerformance`'s table cache first. Not filmable yet: the `-held` tables (nothing gives the stand-in a slipper).

Edit scripts: write them with the Write tool, never a heredoc (this machine's heredocs eat backslashes and quotes; it broke
two scripts this session).

## 3. Things learned that the next hero needs

- **Staging real players:** `HeroIntroductionScene.Others.cs` (new). `StageOthers(nearest, farthest)` copies every other
  player where they stand (render data only) and notes the can; `_others`, `_canAt`. `FrostOther` sets the shader's own
  frost coat. `MiniatureOf` makes a second copy of a staged body. `KeepLensOffOthers(eye, room)` slides a lens off anyone
  it is too near.
- **The others stand ANYWHERE.** Five films had the lens inside somebody's hat or behind their back. What worked: shoot
  from above head height, pick the clearer side of the caster (`CheskaClearSide`), or push the lens off them. Pushing a
  CLOSE shot moved it behind someone; leave close shots alone.
- **`UltimatePhaseView.Draw` MIRRORS AND PULLS IN shots** after the stage computes them (`_mirror`, `_pull`, from
  `ChooseShot`, judged on the real map). Anything a stage places for the authored lens (Cheska's colour panels, anything
  held to the frame) will be wrong on a mirrored shot. Unsolved; the fix is to do such things on the finished frame
  (`PostProcess`, as Phaister's impact frame) or to make those shots exempt.
- **A caster's body can travel.** The clip writes only the bones under the copy's root, and the view samples the clip
  before the stage, so the stage may move `_bodyRoot` (Cheska's skating). It must be home before the end.
- **A stage that must show the end state must not fade.** `leave` is forced to 1 for Dante, for Cheska's ice, for Sean.
- **Hiding the caster's own body:** `renderer.enabled`, not `forceRenderingOff` (the view sets that itself every capture).
- **A lens looking exactly down has no up:** whichever way it drifts becomes "up" and the picture turns. Lean it a fixed
  tenth and never sideways.
- **`IntroductionIsTheWindup`** (`HeroAbility`): an ultimate whose cutscene already shows it land releases with no wind-up
  after that cutscene. Cheska and Sean use it.
- **Sizes and speeds:** he reads "sped up" and "too linear" fast. Give long things time, vary pace with the shape, and when
  he says extend, extend: length is his to give, and he gave it freely once asked.

## 4. Network and rules changes made (all ride on protocol 156, which has not gone out)

`NetSession.ProtocolVersion` 155 to 156, with its reasons in the comment above it:
- lengths: Dante 3.8 to 7.0, Cheska 3.2 to 11.9, Sean 3.4 to 7.0; `UltimatePerformance.MaxSeconds` 9.0 to 13.0;
- ABSOLUTE ZERO after its cutscene freezes at once (her description text changed to match);
- SUPERNOVA after its cutscene starts at the top of its leap.

⚠️ No test has been run on any of it (his editor was open, then closed). `CheskaWikiRuleTests` and `TimedRecoveryTests`
touch her ultimate and may still expect 1.5 s. `ChatAndLobbyChromeTests` asserts protocol 93, stale long before this.

## 5. Working with the owner (from both sessions)

- Show a film the moment a piece exists, 1x then 0.5x, and look at the contact sheet yourself first. Say plainly what has
  not been seen or heard. He answers in a line or two, often while you are still working: take the note and carry on.
- Pitch two or three ideas and let him pick BEFORE building a hero. When he said "surprise me" and got an unpitched idea, it
  missed twice; what worked was asking him the tone ("cheeky but surprisingly strong") and then following his own
  corrections literally. When he draws over a frame or sends a picture, that is the brief: follow the ROUTE, tidy the HAND.
- Short feedback means a small change to the thing he names. When he asks a question that corrects you ("doesnt cheska's
  model have elbows?"), check before answering: he was right.
- Rules, lengths and gameplay get a question first; he answers them in one line. Look and motion just get done.
- Do not stack blind tries. Three unseen attempts at one placement is the point to stop, say so, and measure.
- Every shape in these cuts is a placeholder until he says the staging is right; do not polish blocking.

## 6. What is still open on Paete (from the first handoff; do not lose these)

- NOT SEEN IN A REAL MATCH BY ANYONE: the sky opening on a shipped map, the underground shot under a real court and
  mirrored, the planted arms and their glow in first person, the vine bones' live motion (amplitudes are first guesses),
  the tree facing its caster at the hand-back, his straightened kneeling arms meeting the court.
- Sound: the cutscene plays `ult_b3`. BAKYA BLOOM's sounds are drafted, not installed (he liked a and c, "too harsh"; a
  softer blend `d` was being made); two of its moments have no cue name in the game yet. Thorn Harvest and the live tree
  are silent.
- Body-clip passes for Bakya Bloom and Thorn Harvest were never started.

Sound for batch 1 has not been started at all: no cutscene of the three has a soundtrack, and the method is his ear on a
video with real CC0 recordings (`tools/build_paete_ult_sfx.py`, `tools/mux_paete_ult_video.py` are the pattern).


### Status added 2026-10-10 (later the same day)

- **Sean**: Round two `sean_p5`: `ParolGround.shader` marks, lantern cast on the court, embers and capiz flakes. Weak: mark reads as a decal from the top shot, bare frame after the burst, clouds are smears. Unseen on a real map.
- **Dante**: Round two `dante_p6`: repainted longhorn with seam lights, plate breaks up, he stands on the `stand` crown; LIVE perch (`DanteLonghornPerch.cs`, wind-up dropped, teleport, only Dante collides with it) and blast fists (`DanteDriftFist.cs`) built and unseen. Round three running: z-fight on the centre plate in the top-down shot; owner wants the break-up moved to the rise shot as a swapped pre-fractured model "preferrably dropping with simulated physics".
- **Nemu**: Round two `nemu_p6`: giant Kuro remodelled as one skin (`tools/kubo_kuro_rage.py`, backup `ArtSource/kuro/pet-nemu-ghost.pre-rage.glb`; rerunning `tools/kubo_kuro.py` wipes it), more VFX. OPEN: in a real match the cutscene cuts his head off the top of the frame while the film shows him whole (owner: "still too high, if anything you should raise the camera so the full ghost is always visible"); agent is finding why and framing from his bounds. Nemu films are not trustworthy for framing until then. A live stoop was added by mistake and is to be reverted (`KuroFormTests` asserts taller than 4.2 m).
- **Editor compile error not ours**: `Editor/MapKit/PaeteRunSceneBuilder.cs` (another session, untracked) used `PaeteRunPlayProbe` before it existed; films return NO FILM while the editor cannot compile.

### The shared effects kit (2026-10-10, night)

The owner pointed at the feed instagram.com/vfx_therapy ("see if you can pull off any of those", then "try doing these"). Reference stills are in the main session's scratchpad `refs_vfx/`. Built and proven on Rafi's crash, film `rafi_fx4`, strip `Logs/paete-ability-film/strip_rafi_fx4_crash.jpg`. AWAITING HIS VERDICT; do not roll it out to other heroes before he says so.

- `Resources/Shaders/UltimateErode.shader`: one unlit shader on its own `_Age`. A procedural mask (`_Look` 0 cells, 1 smoke, 2 ink, 3 streak) scrolls along uv.x and the shape is EATEN AWAY by a threshold (`_Erode`), with a burning rim; `_Dark` for comic black shards, `_Radial` for impact discs, `_Dots` for halftone.
- `Runtime/Visual/UltimateFx.cs`: `UltimateFx.Wisp`, `Disc`, `Pop`, `Place`, `Set`, `Play`. Meshes are typed and built at runtime (crown, hook, whip, arc, s, spiral, fanA/B/C, streak, disc, card). Everything is driven by one age float; no clock, no random.
- Technique sources: swept wisp meshes with scrolling erosion (JangaFX), panning trail masks (Saeki Keiichi), comic pop explosions (Sebastien Dulong). The fourth post (ice lotus, gold ring dome, ground crack) is NOT reproduced yet.
- In `HeroIntroductionScene.Rafi.cs` the session also clamped a lens tilt that looked at empty sky for 0.14 s, made the wave ring rise over 0.3 s, and shortened the wall fade and hit glow. White-out at the crash fell from 36.7 percent to 0.7.
- Also filmed that night: `rafi_q6` (shark exits from the centre, no flip, less squiggle; the arch barely reads end-on) and `nemu_p12` (giant Kuro animated on his 18 joint rig in the cutscene and live; a dark blob beside his eye 5.1 to 6.4 s is unexplained).
