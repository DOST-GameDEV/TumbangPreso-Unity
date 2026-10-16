# Basilio (Dante) presentation plan: direction and VFX, 2026-10-16

Status: PROPOSED, waiting for the owner's verdict. Research: [research.md](research.md). Method:
[HERO_KIT_METHOD](../../HERO_KIT_METHOD.md). Mechanics are the Wiki's and do not change here except where section 7 asks.

## 0 · Who he is on screen

**Basilio holds the difficult space and refuses to be rushed.** Montalban, Bernardo Carpio's mountain struggle, a horned
basalt body with molten seams, a gold eye and scar on his left. Everything he does should feel like the ground obeying his
weight, slowly and completely.

**The one idea behind every skill: his body presses, the stone answers.** Never a spell thrown from the hands.

### The effect family (every Basilio effect obeys all eight)

1. **Cause first.** Every effect starts at a contact point of his body (a heel, a fist, a palm, a shoulder) and travels
   through the court. Nothing spawns in the air.
2. **Light lives inside the stone.** Molten orange appears only in cracks and seams, cut INTO the surface (a lit hole, the
   `SoulGlow` technique), never a raised glowing ridge. Basalt is solid, flat shaded, no texture noise (owner refused grain).
3. **Weight is a held beat.** Every cast has a compression hold of 0.12 to 0.35 s before the release. Rock moves in short
   jolts then holds; it never floats or eases gently.
4. **Endings cool, then crumble.** Seam colour steps orange, dull red, grey over 0.4 s; then the stone cracks, drops chunks
   and sinks into the court. No solid ever fades by alpha.
5. **One silhouette per skill.** Unstoppable is vertical (him, standing tall), Boulder is a compact mass in the hand,
   Bastion is a horizontal wall, Continental Drift is a forward line that branches.
6. **The tell is the counterplay.** The bigger the effect, the clearer its warning, readable by the people it will hit.
7. **Palette:** basalt `(.31,.27,.24)`, warm basalt `(.36,.30,.25)`, molten core `(1,.42,.08)`, molten hot `(1,.62,.18)`,
   cooled red `(.55,.16,.06)`, dust ochre `(.42,.32,.22)`. His jade stays on UI and the ward glyph. Never white: the hottest
   pixel is molten hot.
8. **Readable in the 14 m box.** In live play the can, the chalk and every player stay visible; no effect sits over the
   centre of the caster's first-person view.

### His sound family (one recipe per cue, transient / body / tail)

Instruments, all synthesised from scratch per the 2026-10-01 sound authorisation: `grind` (stone stick-slip), `thud` (a
low struck mass), `crack` (fracture ticks, faster as pressure builds), `sizzle` (magma hiss in seams), `rumble` (sub, felt
more than heard), `chunk` (a rock landing), and for the ultimate's emblem ONE motif: a low carabao-horn call (a synthesised
tambuli-like tone, an instrument, not a voice). Light gets no pure tones; he is matter.

---

## 1 · EARTHBOUND (passive): show WHY he moved less

Today nothing on screen explains the passive; a shove simply travels half as far.

| Beat | Time | Body (everyone) | First person | VFX | Sound |
|---|---|---|---|---|---|
| Contact | 0 | He leans INTO the shove: chest forward, a planted heel (new 0.35 s `earthbound-brace` overlay on the legs only) | a short heavy dip, no roll | two heel furrows scraped into the court along the slide, ending where he stops; 6 grit pieces kicked up from the heels | `grind` scrape under the normal shove hit |
| Linger | 0 to 2.5 s | stands | none | the furrows stay, grey, then crumble flat | none |

Rule: presentation only. No extra lockout, the furrow length equals the real slide distance (measured, never faked).
The TAB tray now names it (shipped in this session).

## 2 · UNSTOPPABLE (signature, 40 s): breaking free, then refusing

The idea in one line: **"he shrugs it all off, and stone grows over him that nothing can stick to."** The owner's orbiting
shields and fitted armour (`DanteCarapaceVisual`) are approved and stay; this plan adds the beats around them.

| Beat | Time | Body | First person | VFX | Sound |
|---|---|---|---|---|---|
| Tell | 0 to .15 | shoulders hunch, fists clench at hips (existing `hero-dante-roar` start) | both fists rise into the lower corners | the court under his feet cracks in a small ring, seams lit | `crack` ticks speeding up |
| Release (cleanse) | .15 | chest flings open, head up | fists drop out of frame | **every status he had visibly SHATTERS off**: a shell in that status's own colour and shape (frost plates for Chilled, a ring of pebbles for Concussed, vines for Rooted) cracks and falls; if he had none, a thin dust shell breaks instead so the cast always reads | `thud` plus a glassy shell break per status |
| Armour | .15 to .55 | armour plates rise from the court and lock onto him bottom to top (feet, shins, belt, chest, shoulders), each a 0.06 s jolt | stone knuckle plates appear on the visible fists, nothing at the lens centre | molten seams light up plate by plate as each locks; the approved orbiting shields form last | a `chunk` per plate, rising pitch |
| Linger | 15 s | approved armour and orbit | knuckle plates | **refusal:** when a status would land, the nearest plate flashes, a chip of the incoming status colour splashes off it and falls. Players learn the immunity worked | a dull `clink` |
| Warning | last 2 s | seams cool from orange to dull red, plates tremble | knuckle seams cool | the orbit slows | low `grind` |
| Dissipate | expiry | plates crack and drop to the court, crumble and sink | plates fall off the fists | chunk scatter, dust ring | `chunk` cascade, falling pitch |

## 3 · BOULDER (attacking, 35 s, held slipper for 15 s): a stone in a sandal

The idea: **"he packs a stone around the slipper."** Existing pieces (`DanteBoulderCoating`, `boulder-load`) stay and get
these beats.

| Beat | Time | Body | First person | VFX | Sound |
|---|---|---|---|---|---|
| Tell | 0 to .25 | slipper hand drops to the hip, the other palm cups over it | the slipper hand dips at the bottom right, the free palm closes over it | grit rises from the court into the cupped palm (a short stream of 10 pebbles) | `grind` |
| Release | .25 | the palm presses down; wrist sinks under new weight | a visible 4 cm dip and slow recovery | a stone crust creeps over the slipper from the palm outward in 3 steps, molten seam along the sole | `thud`, small |
| Linger | 15 s | the held slipper is a crusted tsinelas, heavier in every idle sway | same, in hand | seam pulses slowly; a few grains fall off when he runs | quiet `sizzle` loop, near only |
| Warning | last 3 s | | | seam cools to dull red, crust starts flaking | flaking ticks |
| Throw / travel | on throw | normal throw clip | normal | a short grit trail and a heavier arc wobble on the slipper | a low whum on release |
| Contact, player | hit | | | the crust bursts off the slipper into 6 chunks, a pebble ring forms round the victim's head | `chunk` plus a bright ring tone that becomes the victim's Concussed ring |
| Contact, miss | ground | | | the crust cracks off and crumbles where it lands | dry `chunk` |
| Expiry in hand | 15 s | | | crust flakes away | flaking |

**Concussed must be felt (shared status, used by Boulder and Drift).** Today it is only an icon. Victim's screen: a dull
ochre edge vignette, a slow 2 px double-image wobble that matches the existing aim wobble, and a high ringing tone with the
rest of the mix muffled, all fading out over the last second. Everyone else sees a small ring of three pebbles orbiting the
victim's head, slowed to match their slow walk.

## 4 · BASTION (defending, 35 s, 7.5 s): the ground stands up in front of him

The idea: **"he sweeps his arms apart and the court rises into a parapet."** Reinhardt's rule: a strong rim, a clear
middle.

| Beat | Time | Body | First person | VFX | Sound |
|---|---|---|---|---|---|
| Tell | 0 to .2 | stamp, then both arms start low in front, palms down (existing `bastion-brace`) | palms at the bottom of frame | cracks run out sideways from the stamp along the line where the wall will stand | `crack` run |
| Release | .2 to .45 | arms sweep apart and up | palms sweep out to the screen edges | **seven basalt slabs rise out of the court in a ripple, centre first then outward**, each a jolt then a hold, waist high (about 0.9 m). Above them a heat-shimmer field to head height: visible edges and a molten rim, a clear see-through middle | a `chunk` per slab, centre out |
| Linger, follow | 7.5 s | | only the top edge of the slabs at the very bottom of the frame; field edges at the screen sides | as he moves, the slabs DRAG through the court, leaving scrape lines and grit, so "it follows you" reads as earth moving with him | low `grind` while moving |
| Contact (reflect) | each slipper | | | the slab or field point that was hit flashes molten, a crack star stays cooling 1 s on it, 4 chips spray; the slipper leaves with a short grit trail | a hard stone `clack` |
| Warning | last 1.5 s | | | seams cool, slabs sag 5 cm | |
| Dissipate | 7.5 s | | | slabs crack and sink back into the court in the same ripple, outside first | `chunk` cascade |

Readability: the field's middle stays clear enough that a thrower sees him and he sees the can (measured on a film frame,
centre area under 15 per cent covered).

## 5 · CONTINENTAL DRIFT (ultimate, 12 points): the cutscene

### 5.0 · The bar: Paete, Nemu, Phaister and Amihan

Owner, 2026-10-16: *"it has to be as good as paete, nemu, phaister and amihans ult"*. Read from their own scene headers:

| What all four do | Paete | Nemu | Phaister | Amihan | Basilio (this plan) |
|---|---|---|---|---|---|
| A personality beat, not only power | kneels and calls the ground | the possessed player walking normally, then the snap | floats up limp, laughing | bored, eyes shut, taps, shrug; a wink to finish | **patient: he kneels, listens, and knocks twice on the court like a door** |
| The WORLD changes | court sprouts, the light comes down | a gate tears the sky open | the day dies; the sky splits | the monsoon sweeps the whole plaza | **the court breaks into continents on a molten sea** |
| A GIANT from the hero's own myth | the guardian tree and Makiling | the souls from the gate | the eye, the gloves, the doll | the first bird of the creation story | **the horned mountain-beast under Montalban (Bernardo Carpio's mountain)** |
| A CLOSE-UP of its eyes as the climax | the burl's lit eyes | the victim's eyes igniting | the eye locking on the lens | the bird's eyes past the lens | **its lit sockets igniting at the lens** |
| The real VICTIMS on screen | dragged and rooted | possessed, hunted | stagger as it drops | thrown down the lane | **stumbling on their own plates as the continents drift** |
| A finish with weight | wakes, holds them | the snap inside the glitch | ends on the stare | a feather, a wink | **both fists land as one; cut on the contact** |
| Code it took | 3,861 lines, 6 partials | 1,405 lines | 1,291 lines, 2 partials | 1,344 lines, 2 partials | today: 272 lines, blocking prisms |

So the work is the same SCALE as those four, not a polish pass: a modelled creature, a typed density pass in its own
partial (`HeroIntroductionScene.DanteBurst.cs`), the victims staged (`HeroIntroductionScene.DanteTake.cs`), a cut-in seam
shader, and many owner review rounds. Each of the four took 7 to 21 versions; plan for that.

### 5.1 · Critique of the current cutscene (7.0 s, owner-approved beats PUNCH, CONTINENTS, RISE, STOMP)

What is right and stays: the four beats, the plates on a molten sea (the ult's own name made literal), ending exactly on
the fist landing with him still on the creature, the going-under left to live play.

What is wrong:
1. **It opens on an empty frame.** Shot A starts on his rest pose. Every reference opens with the element already moving.
2. **No travelling thing.** The punch's power never visibly goes anywhere; the plates simply begin drifting. Paete's lesson:
   one light travels through every cut so each cut continues the one before.
3. **The longest shot has the least action.** CONTINENTS is 2.25 s of slow drift from a high static view; he is tiny and
   nothing changes for over a second.
4. **The creature is blocking prisms** (its own comment says so) and it is revealed back first. Castorice and Paete both
   showed the summon's FACE first; scale was sold by framing.
5. **The camera crosses the line.** Shot C looks from his front right, shot D from his front left: the action flips sides
   at the most important cut.
6. **Nobody to hit.** The ending composition has no players in it, so the scale of the threat never reads.
7. **No impact frames, no emblem, thin density.** The air is empty between beats compared with Paete's density pass.

### 5.2 · Direction

**One sentence:** *He knocks on the court like a door, then strikes it; his fire runs through the cracks and splits it into
continents, wakes the horned mountain beneath, and it lifts him to deliver the blow.*

**His personality, which none of the other four have:** patience. Everyone else's ultimate starts fast. His starts STILL:
on one knee, palm flat on the court, eyes closed, listening to the ground while the whole match waits for him. *"Refuses to
be rushed."* Two calm knocks; only the third blow is violent. The contrast is the character.

**The travelling thing:** the molten light. Fist, then seams, then the creature's eyes, then up its arm into its fist and
from his chest into his fists. **It always travels left to right across the screen**, and every shot keeps the camera on
the same side of the line (his right, the creature's left).

**The emblem:** Basilio's mark, **two hooked horns over a split stone** (the left horn chipped, like his own scar side). Drawn
three times only: cracked into the court by the punch (0.2 s), lit on the creature's brow as it wakes, and huge behind them
both at the stomp. Each time with the horn-call motif.

**Length:** 7.0 s, the length the owner already agreed for Basilio (2026-10-08); the same as Nemu's. The time is re-spent,
not added: the knock buys the personality, CONTINENTS gets shorter and now carries the victims.

### 5.3 · Beat table (7.0 s)

| Time | Shot and camera | Picture (body acts first) | VFX layer | Sound |
|---|---|---|---|---|
| 0.00 | **1 THE KNOCK** medium, low, his right side, 2.2 m off, very slow push in | **still**: on one knee, right palm flat on the court, eyes closed, head tilted, listening. Dust already hangs in the air; the match is frozen round him | 12 dust motes and grit hanging, 3 at the lens; a faint heat shimmer off the court | cut-in accent: a single low `rumble` from far under the court |
| 0.45 | same | **knock one**: two knuckles rap the court, calm | a thin molten ring runs out under the court surface from his knuckles; every loose grit hops once | a hollow stone knock, deep, with a long echo |
| 0.80 | same | **knock two** | a second ring, wider; the hairline cracks round his hand glow and STAY lit | knock two, and something far below answers: a muffled thud |
| 0.95 to 1.15 | insert, 0.6 m on his face | **his gold eye opens** (his left, the scarred side) | the eye's gold catches the seam light from below | the horn-call motif, short and low |
| 1.15 | **2 THE PUNCH** back to the side angle, now 1.4 m | he rises to a crouch, fist cocked behind his head; the hanging grit pulls in toward the fist | grit stream into the fist, 14 pieces | swell drawn in |
| 1.30 to 1.50 | same | **held compression**: fist trembles at the top, knees sink 6 cm | everything holds | the swell stops: 0.15 s of near silence |
| 1.55 | same, 4-frame hard shake | **the punch lands** just ahead of his feet | 2-frame impact frame (black ink on molten orange, radial lines); shockwave ring 3 m; the emblem's cracks splay out from the fist for 0.25 s | `thud`, cut dead |
| 1.70 | **3 THE CONTINENTS** one continuous crane from the fist up and back to 8 m, following the light | he stays crouched, fist in the court, visible centre left all shot | **six molten seams race out from the fist** (left to right first), the camera rising with them; plates jolt apart in two steps (1.95, 2.30); the sea's glow comes up under the gaps; backdrop grades to 0.7 | `crack` runs, `grind` as plates part |
| 2.30 to 3.10 | same, slowing | **the real players, staged on the outer plates, stumble**: arms windmilling, one drops to a knee, all drifting AWAY from him on the molten sea. The court is visibly theirs no longer | rising motes from every seam (30 typed); dust rings break off each plate edge; grit falls off the plate rims into the sea and flares | `rumble` builds |
| 3.10 | same | the brightest light DIVES into the widest seam, between him and the targets | a flare down into the seam | a sizzle drop |
| 3.30 | **4 THE RISE** cut low to that seam, 2 m off, tight; same screen side | **face first**: two hooked horn tips break the surface, rock peeling off them | chunks fall off the horns in jolts | `grind` heave one |
| 3.75 | same, the lens holds | the brow comes up; **its eyes ignite as lit sockets right at the lens** (the climax close-up the other four all have); the emblem lights on its brow | eye light cut in; a burst of 9 rays from behind its head; an impact frame on the ignite | horn-call motif, full |
| 4.00 to 5.20 | dolly straight back and up through **three heaves** (4.0, 4.5, 4.95), his plate riding its back | he braces low on its back, then stands as it settles | a curtain of falling rock off its back (24 typed), rings where its hands slam down, the targets' plates shoved aside | `grind` heaves two and three, `thud` per hand plant |
| 5.30 | **5 THE STOMP** wide and low from in front of the creature, its left, the same side as shot 4 | it raises its right fist; he loads both fists over his right shoulder; the targets small in the far court, looking up at it, bracing | the light runs from its eyes up its arm into its fist, and from his chest down his arms into his fists | swell drawn in again |
| 5.30 to 6.60 | slow creep in | the targets turn to run (their own existing shocked clip) | molten rays fan behind the pair, turning slowly; motes rising; the giant emblem fades in behind them | `rumble` peak, `crack` ticks speeding |
| 6.60 to 6.85 | same | **held**: both fists at the top, trembling; the story clock slows to a fifth (Amihan's hang, his version) | everything holds | silence |
| 6.88 | same, hard shake | **both fists come down as one**, his onto its back, its into the court | 2-frame impact frame; a lit seam shoots forward toward the targets; ring; the emblem flares and cracks | the biggest `thud` of his kit |
| 7.00 | cut to live | he is still on its back in the stamp pose | stage whole on the last frame, as the owner asked | tail carries into live play |

### 5.4 · The density pass, typed per beat (take the kinds from Paete, never his rows)

| Kind | Basilio's version | Count |
|---|---|---|
| Hanging grit | angular basalt pieces, each its own size, hanging still at the knock, hopping on each knock, pulled into the fist | 14 |
| Knock rings | thin molten rings running under the court surface from his knuckles | 2 |
| Shockwave rings | thick dust-ochre ring with a molten inner edge, flat on the court | 6 (punch, three heaves, stomp, plus one at the plate split) |
| Seams | branching, uneven, one dominant line and short side chips, never parallel | 6 main, 18 chips |
| Rising motes | small molten embers rising from seams | 30 |
| Rock curtain | chunks sliding off the creature's back as it rises | 24 |
| Rays | warm molten rays behind the pair at the stomp, slowly turning | 9 |
| Impact frames | ink on molten orange, radial lines, 2 frames each: the punch, the eyes igniting, the stomp | 3 |
| Emblem | horns over split stone, three appearances | 3 |
| Near layer | grit and embers on the lens line in every shot | 4 to 6 per shot |
| Grade | backdrop to 0.7 from the seams on, back toward 0.9 at the stomp | 1 |

### 5.5 · The creature, modelled (replacing the blocking prisms)

A typed glb like Paete's props (`tools/build_dante_props.py`, every part by hand): **a horned basalt mountain-beast**, the
same family as Basilio (his horn shape, molten seams cut in, a chipped left horn), blocky TUMP shapes, no realistic anatomy,
no texture grain. Named nodes for what moves: horns, brow, jaw, two arms with fists, three back plates that shed rock. Eyes are
sunken lit sockets on the real surface (measure the mesh, seat them, per the method's eye lesson). Big things move little:
its life is in breath, seam pulses and rock shedding, not in turning. Owner turnaround verdict before it goes into the scene.

## 6 · CONTINENTAL DRIFT: live play after the hand-back (where the scale must read)

This is what most players actually experience, from their own first-person view, so it must look map-wide.

| Beat | Time after hand-back | Everyone | His first person | Sound |
|---|---|---|---|---|
| Going under | 0 to .4 | the creature sinks into the court behind and under him, rock sliding off, as the owner asked | his stamp follow-through continues (existing), creature not in his lens | `grind`, falling |
| **The fuse** (the warning, existing 0.4 s) | 0 to .4 | a molten seam races forward from him along the cascade line and leaves **five glowing crack-stars marking exactly where each blast will land**, so victims can read and step out | visible ahead of him | a fast `crack` run |
| Blasts 1 to 5 | every 0.30 s | at each mark: a slab of court heaves up 0.35 m in a jolt and tilts, molten light under it, 5 big chunks thrown (typed per band, no two alike), a dust ring the exact size of the hit area. Visual size never lies about the hit area | camera kick per blast, scaled by distance | each blast a `thud` plus `chunk`, rising a semitone each, the fifth the heaviest |
| Map-wide | during the cascade | **every player's camera shakes by distance; dust and grit fall in each viewer's own view from above; loose pebbles on the court hop**; a deep rumble everywhere | | `rumble` court-wide |
| Hit | per victim | Concussed presentation (section 3) | | ringing on the victim only |
| Aftermath | 1.2 s tail plus 3 s | slabs drop back, the fault line stays as a scar that cools orange, red, grey, then crumbles flat | | settling `chunk`s |

## 7 · Questions for the owner (one batch)

1. **Concussed length.** The Wiki says 75 per cent slower for **5 s** with ringing and a HUD effect; the code applies **2.5 s**
   (Boulder and Drift text also say 2.5 s). Make it 5 s, or keep 2.5 s?
2. **The knock.** Open still, on one knee, listening, two calm knocks, his eye opens, THEN the punch? (Kept at your agreed
   7.0 s by shortening the continents.)
3. **The victims.** The real players stumbling on their own drifting plates, then small in the far court bracing as the fists
   come down (still ending on the fist, no blast shown)?
4. **Bastion's look.** Waist-high basalt parapet with a see-through heat field above it, or full stone slabs with gaps?
5. **Unstoppable.** Keep the approved shields exactly and only add the shatter-off, the refusal flash and the cooling warning?
6. **The emblem and motif.** Two hooked horns over a split stone, with a low carabao-horn call as its sound?
7. **The creature.** A horned basalt mountain-beast in his own family (his horn shape, chipped left horn). Any picture you
   want it to look like?

## 8 · Build order for the implementing session

1. Answers recorded here (a table: answer, quote, what it becomes).
2. Concussed status presentation (shared): victim screen, ring sound, observer pebble ring.
3. Earthbound furrows and brace overlay.
4. Unstoppable shatter-off, plate assembly, refusal, warning, crumble.
5. Boulder crust lifecycle and burst.
6. Bastion parapet, drag, reflect crack, sink.
7. Drift live: fuse marks, five typed heaves, map-wide dust and shake, scar.
8. The creature model (turnaround to the owner first).
9. Cutscene: new beat table in `tools/author_ultimate_intros.py` (`_dante`), `--preview` storyboard, then
   `HeroIntroductionScene.Dante.cs`, then the density pass.
10. Sounds, one recipe per cue, loudness table against the current cues.
11. Films per ability and the ultimate: his screen, the court, a caught player; versioned mp4s to the owner.

Each step: focused checks only (`docs/TESTING.md`), no protocol bump for cosmetic work, `Staged` respected in replay,
nothing in the cutscene on `Update`, no em dashes, no co-author trailer in this repo.
