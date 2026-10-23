# The TUMP sound plan: generating SFX and music in our theme

Owner, 2026-10-23: *"this chat will be the benchmark for future sfx and music generation"*, *"i will train u to get
it into the theme that i want"*, *"plan around generating smth in our theme based on ur research and understanding
of our game"*, *"js generate in any way u can"*.

Everything that ships is GENERATED. Real recordings are only measured (`research.md` section 0 says why the old
generated sounds failed: noise and sines with no object behind them, and no shared world).

## 1. The identity in one line

**The street band of a Tondo eskinita at dusk.** Kids playing tumbang preso with a tin can, a chalk box, tsinelas,
and whatever the alley gives them. Warm, toy-like, a little mischievous, never harsh. Pleasant everyday sounds
(Untitled Goose Game's lesson) with one big signature per moment (the can, the taya).

## 2. The palette (round 2: the owner rejected round 1's toy band, see `tondo/review.md` taste log)

**Round 2 palette, current:** a barangay rondalla, designed from string physics (`tump_synth.string_note`, exact
harmonics, so always in tune): the **bandurria** (double steel courses, plectrum, tremolo) carries the tune, the
**guitar** plays the chords (balitaw: bass on the beat, chords off it), the guitar's **bass** strings play the root.
The street joins only on purpose: **bamboo** (kalutang, tuned to chord tones) and the **balde** (an upturned plastic
bucket, its thump ending on the chord's root). **Kamay** (hands) and **kalye** foley stay, with no pitched parts.
No bells, no glockenspiel, no triangle, no xylophone, no marimba. At most three parts at once, one job each.

### Round 1 palette (rejected: "WHY IS THERE A BELL", "0 harmony", "mashed a bunch of unrelated audios")

| # | Instrument | What it is in the world | How it is generated | Learned from (measured, never shipped) |
|---|---|---|---|---|
| 1 | **Lata** | the tin can, the game's own object, and its motif voice | modal synthesis: the can's measured inharmonic partials and decays, a hard-tip strike | Freesound 178183, 500029 |
| 2 | **Toy glock** | the music box under the laundry, the count's sparkle | modal: measured glockenspiel partial ratios and decay per note | VSCO 2 CE glockenspiel |
| 3 | **Kahoy** (wood) | marimba bass, xylophone giggles, claves and log drum on the street rhythm | modal: measured bar modes per instrument, mallet hardness per use | VSCO 2 CE marimba, xylophone, claves, log drums |
| 4 | **Rondalla pluck** | the bandurria and guitar of a barangay band | Surge XT (plucked string model) or the modal engine with measured harp decays | VSCO 2 CE harp |
| 5 | **Kamay** (hands) | claps, the huddle's count | coloured-noise bursts on each measured clap's spectrum, attack, decay and flams | Freesound 463687, 561089, 789991, 860150 |
| 6 | **Kalye** (street foley) | chalk on concrete, tsinelas slaps and scuffs, a wooden door's creak and latch | grains on the chalk's measured spectrum and rate; stick-slip at the creak's measured pulse rate through the door's measured resonances; metal and wood modal clicks | Freesound 440442, 350307, 560366, 196148, 757194, 213404 |
| 7 | **Hangin** (air) | the soft bed under a scene, a warm swell | Surge XT, kept low and dark, never leading | (none: a synth sound by design) |

## 3. The rules (from `research.md` section 4)

1. One band, and it lives in the alley. 2. C major pentatonic, 100 bpm, on the scene's own beat table.
3. One chord at a time (I-IV-V7), every note damped at the change. (Round 1's do-mi-sol-do on a glock is retired.) 4. One room (`tump_synth.alley_ir`).
5. Lanes: lows for kahoy bass only, each cue its own band, a pocket in the music for each cue, 300 to 3000 Hz kept
   clear for any human voice. 6. Foley in the cue, tuned answers in the music at the cue's time. 7. Variation by
   different strikes and scale steps. Loudness: the scene's mix below 0 dBFS true peak, about -18 LUFS.

## 4. The pipeline (the skill-to-be)

0. **Spot the scene first** (round 3): watch it frame by frame, read what plays before and after it, measure the key
   and tempo of what follows, and write the beat sheet (`tondo/spotting.md`): on screen, story, feeling, music, sound.
   Every sound needs an on-screen reason or a job in the music. The music tells the scene's arc and ends on the
   dominant of whatever plays next, at its tempo.
1. **Reference**: find a CC0 recording of the real object; log it in a `*_sources.json`.
2. **Analyse**: `tump_synth.modal` / `texture` / `creak` turn it into numbers, saved in a committed profile JSON.
3. **Generate**: a recipe per cue from the palette, through the one room and the shared glue.
4. **Place**: render the scene with its real cue times (`MapFilmTests.<Map>Recording` cues.txt) and the map's bed.
5. **Measure**: peak, true peak, LUFS. Numbers keep it safe; they never approve it.
6. **Owner listens**: the verdict per cue ID goes into the review sheet's taste log, and only named rows change.
7. **Learn**: every verdict becomes a rule in the skill ("T-04 too thin" becomes "claps need a body under 1 kHz").

## 5. Rounds

- **Round 1 (now)**: the Tondo film, all five files regenerated from the palette, one before/after video.
- **Next rounds**: only what the owner names, until the Tondo film is liked.
- **Then**: `.claude/skills/tump-sfx/SKILL.md` from what worked, then the other four films, then gameplay
  chunks (can hit, tsinelas throw and land, tag, UI), then music: loopable tracks from the same seven instruments.
