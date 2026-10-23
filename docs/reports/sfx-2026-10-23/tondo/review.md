# Tondo cutscene (Eskinita's opening film): the SFX review sheet

Mark a row by its ID ("T-04 too thin"). Only the rows the owner names change in the next round.
Method and theme: `../plan.md`, `../research.md`. Build: `py -3 tools/build_tondo_film_sfx.py` (generated from
measured references in `tools/tump_sfx_profiles.json`; nothing recorded ships except T-06, which was already in the game).

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
