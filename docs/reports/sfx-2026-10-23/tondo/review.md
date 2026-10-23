# Tondo cutscene (Eskinita's opening film): the SFX review sheet

Mark a row by its ID ("T-04 too thin"). Only the rows the owner names change in the next round.
Method and theme: `../plan.md`, `../research.md`. Build: `py -3 tools/build_tondo_film_sfx.py` (generated from
measured references in `tools/tump_sfx_profiles.json`; nothing recorded ships except T-06, which was already in the game).

## Round 2 (2026-10-23): everything revamped, a rondalla in one key

Watch: `r2-after.mp4` (with the alley bed). One mp3 per cue in `cues/` (`*_r2.mp3`).

What the owner said about round 1, and what round 2 does about each:

| Owner on round 1 | Cause found | Round 2 |
|---|---|---|
| "WHY IS THERE A BELL", "NO BELL THERE" | the music box lullaby, the four door chimes and the taya run were a glockenspiel; a triangle too | no bells at all: no glockenspiel, triangle, xylophone or marimba |
| "ang saket sa tenga theres 0 harmony" | the tuned sounds came from measured partial lists full of untuned peaks; chords rang into the next chord; the creak and the skid squeak were pitched sweeps at random pitches | strings built from exact harmonics of A440 notes (in tune by construction); every note damped at the next chord change; foley has no pitched parts |
| "ang gulo talaga", "less is more" | up to seven instruments at once, each with a busy line | three parts with one job each (guitar chords, bass root, bandurria tune); the tune rests while the chalk, the claps and the kid's voice speak; bamboo only twice, balde only on the big hits |
| "TRY TO MAKE IT SOUND FILIPINO" | a toy orchestra, nothing local | a barangay rondalla: bandurria tremolo for the tune over a guitar in balitaw style (bass on the beat, chords off it), I-IV-V7 in C like most Filipino folk songs; street bamboo and an upturned balde |
| "make ur own" | measured instruments | the instruments are designed from string physics in `tools/tump_synth.py`, not copied from samples |
| "i like the bird" | the alley's own sparrow bed (`alley_bed_sparrows`) | untouched |

The cues in round 2:

| ID | Round 2 | Status |
|---|---|---|
| T-01 | the rondalla on the beat table: guitar picking C, G into the chalk; balitaw under the chalk with two bamboo backbeats; the tune in long tremolo notes over the doors (E, C, B, one per chord); a soft G7 and the bass walking G A B under the claps to C on YA; a bandurria giggle after the voice; A minor and G strums with the balde as the taya squares up; home on C, F, C | draft |
| T-02 | chalk grains, the tsinelas slap and rubber scuff on the punch (the pitched squeak is gone) | draft |
| T-03 | latch tick, an unpitched hinge creak, the bump on its stop (no pitched parts, so the per-seat pitch cannot clash) | draft |
| T-04 | unchanged from round 1 (three small hands), quieter (peak 0.75) | draft |
| T-05 | a balde thump on C and one full guitar strum of C, then after the kid's "Taya!" a bandurria tremolo on G and C | draft |

Measurements (the harmony checks are new in `tools/tump_synth.py`: `roughness` is the Plomp-Levelt / Sethares model of
beating between partials, the "saket sa tenga" stand-in; `in_key` finds the sounding notes, not the overtones):

| File | Roughness median / p90 | Sounding notes in C major (median / p10) |
|---|---|---|
| score, before | 0.145 / 0.439 | 1.00 / 1.00 |
| score, round 1 | 0.111 / 0.263 | 1.00 / 1.00 |
| score, round 2 | 0.098 / 0.200 | 1.00 / 1.00 |

Every version's notes were in the key; what hurt was harsh, untuned overtones and clutter, which roughness sees.
The round 2 sting reads rougher (0.104) than the old thin glock run (0.04) because a guitar chord has more partials;
it is a plain C major chord.

| Mix with the alley bed | Integrated | True peak |
|---|---|---|
| Round 2 | -18.0 LUFS (the plan's target) | -0.6 dBTP (the kid's recorded voice at YA sets this ceiling) |

## Round 1 (2026-10-23): all five generated files replaced

Watch: `r1-after.mp4` (round 1, with the alley's ambience bed underneath) against `r0-before.mp4` (the old sounds,
same picture, same bed). One mp3 per cue in `cues/`. The old wavs are in `before/`.

| ID | Cue | Intended feel (one line) | File | Recipe (`tools/build_tondo_film_sfx.py`) | Call site: volume, pitch | Before | After | Status |
|---|---|---|---|---|---|---|---|---|
| T-01 | Score | a toy street band's lullaby at dusk: music box, rondalla plucks, marimba, claves, landing on every cue | `Resources/Sfx/sfx_film_tondo_score.wav` (20.5 s, peak 0.70) | `tondo_score` | `MapFilm.Sound` score level 0.85, faded with the film (`TondoFilm.cs:35` names it) | `before/sfx_film_tondo_score.wav` | `cues/T-01_score_r1.mp3` | draft |
| T-02 | Chalk | a kid running a stick of chalk along concrete, steps pressing it, a tsinelas skid on the camera punch | `sfx_film_tondo_chalk.wav` (2.7 s; skid at 2.15 s) | `tondo_chalk` | `TondoFilm.cs:244`, 0.7, 1.0 | `before/sfx_film_tondo_chalk.wav` | `cues/T-02_chalk_r1.mp3` | draft |
| T-03 | Door | a light wooden house door: latch, hinge creak, a soft bump on its stop | `sfx_film_tondo_door.wav` (0.9 s; bump at 0.5 s) | `tondo_door` | `TondoFilm.cs:311`, 0.55, 0.92 + 0.05 per seat | `before/sfx_film_tondo_door.wav` | `cues/T-03_door_r1.mp3` | draft |
| T-04 | Clap | three small hands clapping together on each syllable of "Maiba taya" | `sfx_film_tondo_clap.wav` (0.3 s) | `tondo_clap` | `TondoFilm.cs:363`, 0.8, 1.0 + 0.04 per clap | `before/sfx_film_tondo_clap.wav` | `cues/T-04_clap_r1.mp3` | draft |
| T-05 | Taya sting | the count lands: the tin can rings, the toy glock runs do-mi-sol-do an octave up, a wooden floor under it | `sfx_film_tondo_taya.wav` (1.7 s, peak 0.60) | `tondo_taya` | `TondoFilm.cs:364`, 0.8, 1.0, with a camera punch | `before/sfx_film_tondo_taya.wav` | `cues/T-05_taya_r1.mp3` | draft |
| T-06 | Kid's "Taya!" | a real kid calling it | `Art/audio/ambience/sfx_life_kid_taya_1.wav` (0.55 s, a recording already in the game) | none (recorded; never synthesise a voice) | `TondoFilm.cs:364`, 0.75 (from `SidewalkLife.KidTaya[0]`, `:165`) | unchanged | unchanged | kept: allowed by `docs/MAP_FILM_METHOD.md` rule 4; the sting leaves 300 to 3000 Hz for it |
| T-07 | Map ambience | the barangay under the film: neighbourhood, street, kids, sparrows, wind | `Art/audio/ambience/alley_bed_*.wav` (`AlleySoundscape`) | `tools/build_eskinita_life_sfx.py` (recorded, CC0) | `AlleySoundscape` default gains x BedsGain 0.7 | unchanged | unchanged | not touched this round; in the videos at its default gains with every location weight taken as 1 |

### How round 1 ties the cues together (the owner's "cohesive" note)

- One band: every tuned sound is the same generated toy band (glock, xylophone, marimba, harp, claves, log drum,
  triangle); every foley sound is learned from a real object of the alley.
- One key, one motif: C major pentatonic at 100 bpm. The lullaby opens on do-mi-sol-do; each of the four doors rings
  the next note of it on the glock, exactly on its door's time; the taya sting completes it an octave up.
- Tuned answers on the cue times: a xylophone flick on the chalk skid, claves plus a xylophone step C-D-E on the
  first three claps, the marimba and log drum floor on YA. Strums are skipped near each door so the door is heard.
- One room: every file goes through the same small concrete alley (`tump_synth.alley_ir`) and the same glue.

### Measurements (they keep the mix safe; they do not approve it)

| Mix (`tools/render_map_film.py Eskinita`) | Integrated | Peak before the renderer's safety scaling |
|---|---|---|
| Before, with the alley bed | -19.6 LUFS | +2.5 dBFS (it clipped: the YA clap, sting, voice and score stacked) |
| Round 1, with the alley bed | -16.1 LUFS | -0.65 dBFS (true peak -0.6 dBFS) |
| Round 1, film only | -16.2 LUFS | -0.48 dBFS |

Round 1 is about 3.5 LU louder than before. If it feels loud against the match, the score's level (LEVEL in the
build) is the first knob.

⚠️ Not yet checked: a real in-game recording with the live ambience and listener position (the handoff asks for one
before a version is called done). The videos mix the beds at estimated weights.

## Taste log (the owner's verdicts, each one becomes a rule in the TUMP SFX skill)

| Round | ID | Owner said | What changed |
|---|---|---|---|
| 0 | all | "this game has horrible sfx rn"; "its js noise and a bunch of sounds taht dont have a theme and dont work well together or dont support each other" | round 1: one band, key, motif, room; everything generated from measured real objects |
| 1 | all | "ang panget pareho" (could not tell round 1 from before); "u js mashed a bunch of unrelated audios tgthr"; "i like the bird"; "WHY IS THERE A BELL"; "TRY TO MAKE IT SOUND FILIPINO"; "ang saket sa tenga theres 0 harmony"; "ang gulo talaga"; "completely revamp everything"; "make ur own"; "sometimes less is more" | round 2: no bells; a rondalla built from exact harmonics; one chord at a time; three parts; no pitched foley; harmony checks before the owner listens. **Rules for the skill:** never a bell or music-box timbre in TUMP unless asked; Filipino means rondalla strings and street percussion; harmony first, then fewer parts; a measured partial list is not an instrument |
