# Ability direction: the baseline every skill rework follows

Owner, 2026-09-25, while Amihan's kit was being built: *"thoroughly try to direct all vfx and sfx
of the skills so that it will look cohesive, good and satisfying"*, *"it will be the baseline for
all rework of skills so lock in"*, *"i want it to be STEPS above all current vfx and sfx and
animation"*.

This file is that baseline. Section 1 to 7 hold for EVERY hero; section 8 is Amihan's reading of
them, and is the worked example. Research behind it: [research.md](research.md). The standing
rules it sits inside are `docs/VISION.md` § 2 (readability budget) and § 3 (a power is read by
looking), `docs/Art_Direction.md` § 0 (the cute blocky world) and `AGENTS.md` ("each hero/action
needs its own form, silhouette, casting, movement and sound").

## 1. Every ability is six beats, and every channel lands on the same beats

| Beat | What it is for | Body (TPP) | Hands (FPP) | VFX | SFX | Camera and HUD |
|---|---|---|---|---|---|---|
| **1. Tell** (0.05 to 0.2 s) | the room learns WHO is about to do WHAT | anticipation pose, the opposite of the release direction | hands draw back or gather | a small gather at the hands in the hero's hue | an inhale: short rising air, the hero's own timbre | the tile flashes, the reticle shows WHERE |
| **2. Release** (1 frame) | the moment it happens | the snap into the strike pose, held 0.08 to 0.2 s | the snap, with a view kick | the brightest frame of the effect, at the source | the transient: the loudest 20 ms of the cue | a small punch on the caster only |
| **3. Travel** | the thing moving through the world | follow-through, weight settling | hands follow the thing out | the travelling shape: its silhouette is the ability's | the body of the cue, Doppler or panned with the object | none |
| **4. Contact** | what it DID to someone | (victim) the hit reaction for the status it applied | (victim) a view shove | a contact burst ON THE VICTIM in the caster's hue, then the status tell | a contact hit distinct from the cast, heard by the victim louder | the status icon appears over the victim and on their own HUD |
| **5. Linger** | what is still dangerous | recovery pose | hands return | the persistent part at low value, edges only | a quiet loop or nothing | the status timer |
| **6. Dissipate** | "it is safe now" | none | none | the edges THIN and break into motif particles | a soft tail, never a second hit | the icon leaves |

A beat with no picture or no sound in it is a hole: it reads as lag (the release) or as a bug (the
dissipate). A beat that two channels place at DIFFERENT times reads as sloppy. So the times are
written once, as constants on the ability, and every channel reads them.

## 2. Shape language

- **The silhouette is the ability.** If every colour were removed, the shape and its motion must
  still say which ability it is. Each hero owns its element's shapes and no two of one hero's
  abilities share a silhouette.
- **Bright thin edges around a darker middle.** An effect is drawn as its EDGES (rims, crescents,
  ribbons, cracks, streaks) with a quieter body. A filled translucent disc is a puddle
  (`VISION.md` § 2 rule 3), and two of them overlapping is mud.
- **Depth layering.** Big effects have near, middle and far layers moving at different speeds.
- **A motif particle per hero,** from their own culture and story, never a generic spark.
- **Fade by thinning,** not by dimming a filled area. The line gets narrower and breaks up.

## 3. Colour and value

- **The hero's hue owns the effect**, at the `UiTheme.*Bright` value on the ground (the base
  accent is for UI). Never another hero's hue (`VISION.md` § 3, `UiTheme` hue law).
- **A value ladder of three**: a near-white CORE line (the hue at 8 to 15 per cent saturation), the
  hue BODY, and a darker INK edge a third of the body's value, which is what keeps the effect
  readable on the lit PEAK-style court and ties it to the ink outlines on every character.
- **One warm accent** in a cool effect, or one cool accent in a warm one. The eye follows it.
- **Nothing blows to white.** `AbilityShowcaseProbe` fails a frame over 12 per cent blown; the
  core line is thin precisely so the brightest value covers the least area.

## 4. Sound

- **Three layers per cue**: transient (0 to 30 ms), body (the ability's own texture, 0.1 to 1 s),
  tail (a decay that says it is over). Each ability gets its own recipe in the hero's generator,
  and no two abilities anywhere share one (`tools/generate_skill_audio.py`'s header rule).
- **Cast, travel, contact and end are separate cues**, because they happen at different places and
  times. The contact cue is the one the victim must hear: it is louder at the victim.
- **The element has a family sound, the ability has its own verb.** Wind is air moving; a dash
  is a short whoosh passing, a gale is a sustained rushing front, a flight is lift and flutter,
  a storm is pressure building then a wall.
- **Everything world-side goes through `NetCue`** and is played once per peer
  (`HeroAbility.CastCue`'s note).
- **Ultimates warn allies and enemies differently** (Valorant): the theme is the same but the
  voice line differs, and the gather has a sound anyone on the court can place.
- **Voices are human recordings only.** The lines ship as text rows until recorded
  (`docs/HUMAN.md`).

## 5. Animation

- **Poses first, held.** Three to five strong shapes per cast, each held 0.08 to 0.25 s; the
  moves between are fast. The strike segment hangs then snaps (`ClipBuilder.PunchAt`).
- **One direction per hero** (`tools/author_hero_action.py`'s table): the body tells the element
  before the effect does.
- **TPP and FPP agree**: the first-person hands do the same gesture, at the same beat, as the body
  the other three players see (`ViewmodelArms` actions).
- **Recovery is part of the clip.** Every cast returns cleanly to rest; an interrupted cast
  recovers both hands (`RosterArmGeometryTests`).
- **Never depend on mesh details.** Animation keys the rig's bones only, so a model rebuild
  (owner, 2026-09-25: "im probably still gonna improve model") re-bakes from the same table.

## 6. Status effects are part of the ability's picture

The owner's status table (2026-09-25) is the one list: **Whirled**, **Chilled**, **Frozen**,
**Tagged**. Each has:

- **an icon** (drawn in the ability-icon family, one silhouette each) shown over the victim and on
  their own HUD with its tooltip and a draining ring;
- **a body tell** readable without the icon (Whirled: a small spiral ring of air round the waist
  and the slipper hand shaking; Chilled: frost at the feet and a slowed idle; Frozen: the ice coat;
  Tagged: the caught mark);
- **a start cue and an end cue**, quiet, on the victim.

The status belongs to the element that caused it, not to the ability: any wind ability that
Whirls someone produces the same Whirled, the way any ice freeze produces the same coat.

## 7. The ultimate cutscene (the introduction)

Four beats, from the research: **who** (a wide that places them), **intent** (one close-up of
eyes or hands with speed streaks), **gather** (the element builds round the signature pose while
the stage grades down behind them), **release** (the power leaves; the last pose is the live
ability's first). Voice inside the gesture. The stage is theirs: a backdrop only in the
introduction's render copy. The live ultimate's first frame is the introduction's last.

## 8. Amihan: the worked example

### 8.1 Palette

| Role | Hex | Why |
|---|---|---|
| Core line | `f4ffe9` | the wind hue at low saturation: bright without being white |
| Body | `a6ec84` | between `HeroWind` 88e35a and `HeroWindBright` c3f5aa |
| Ink edge | `2f6b2a` | a third of the body's value, the same family |
| Warm accent | `fff1d6` cotton, `f2c14e` brooch gold | cream cotton bolls and her gold brooch |

Hue 100 stays clear of Dante's jade (137) and Cheska's mint (170): wind is YELLOW-green, never
blue-green.

### 8.2 Motifs

- **Cotton fibre and bolls** (Vigan's Binatbatan: beating cotton pods to free the fibre): the
  motif particle of every wind effect of hers. Soft cream tufts and single threads.
- **Abel threads**: thin straight lines of her cloth colours carried in the wind.
- **The kasikus whirlwind** (graduated rectangles radiating from a centre, the binakol pattern
  woven to turn away spirits): the shape of her Whirled mark and the ground sigil of her
  ultimate. It is the one place a pattern repeats, because it IS a woven pattern.

### 8.3 Her four abilities through the six beats

| | Tell | Release | Travel | Contact | Linger | Dissipate |
|---|---|---|---|---|---|---|
| **Quick Dash** (signature) | 0.10 s crouch, weight back, arms swept back; a small ring of air at the heels | she is flung forward, one arm leading; a bright slipstream line opens | a tube of three streak ribbons behind her and cotton pulled into her wake; the whoosh passes | a crescent of air bursts on each body she passes, they are shoved and Whirled | two ribbons curl and hang for 0.4 s along her line | the ribbons thin to threads and drop cotton |
| **Updraft** (attacking) | arms down, palms to the ground, a spiral of air lifts dust and cotton | she springs up on a column of wind | while aloft: a slow ring of air under her feet, her sash and hair lifted, a soft pulsing rush | none | the ring; a shadow on the ground where she is | on descent the ring folds up into her feet and puffs out |
| **Whirlwind** (defending) | a wide wind-up, both arms swung across the body | a two-arm sweep throws an ARC of gale forward | a curved front of five ribbons, bright leading edge, rolling forward along the ground, carrying cotton and dust | the front breaks round each body it hits, they spin and Whirl | none (it is one front) | at 2.5 s the arc frays into threads |
| **Storm Surge** (ultimate) | the introduction; then 2.5 s live: she plants, the sky darkens, a fan of streaks on the ground rushes out showing exactly where the wind will go, pressure rising | the fan fires: a wall of ribbons and a kasikus sigil under her | the wall sweeps the whole fan at speed, carrying bodies and slippers | bodies lifted and carried toward the edge | the sky clears over 1.5 s | the wall frays into cotton and threads far out |

### 8.4 Sound recipes (one per ability, none shared)

- **Quick Dash**: a tight band-passed noise whoosh with a fast pitch drop (a thing passing), a
  cloth snap transient, a short tail.
- **Updraft**: a rising low rumble of air (the column), a fabric flutter loop while aloft, a soft
  settle on landing.
- **Whirlwind**: a rolling rush that swells and pans with the front, a swirling phaser on the body,
  a fraying hiss at the end.
- **Storm Surge**: gather = a pressure rise (filtered noise opening over 2.5 s with a slow beating
  drone); release = a deep wall of air with a crack; theme bed under the introduction.
- **Whirled** (status): a short spinning whistle on the victim. **Chilled**: a crisp frost tick.
  **Frozen** and **Tagged** keep their existing cues.
