# Tondo cutscene (Eskinita's opening film): the SFX review sheet

Mark a row by its ID ("T-04 too thin"). Only the rows the owner names change in the next round.
Method and theme: `../plan.md`, `../research.md`. Build: `py -3 tools/build_tondo_film_sfx.py` (generated from
measured references in `tools/tump_sfx_profiles.json`; nothing recorded ships except T-06, which was already in the game).

## Round 5 (2026-10-23): nothing eats itself

Watch: `r5-after.mp4`. Music alone: `r5-music-only.mp3`.

Owner on round 4: "grabe ang deep pa ren and out of tune", "especially the sound in the start it feels weirdly strum
and deep", "like it feels like its eating itself".

What was measured first: every string's overtones are within 0 to 5 cents of exact (tighter than a piano's stretch), so
the strings themselves were not out of tune. What "eating itself" was: the same pitch sounding twice at once, which
partly cancels and throbs:
- the opening started on the guitar's low A2, doubled by the bass's A2, under a C#4 picked twice while the first still
  rang (on a real guitar that is one string, and picking it again stops the old note);
- the YA hit carried its own low A under the score's low A and the guitar's;
- the room's early reflections had two inverted (a comb filter: hollow, phasey).

Round 5: `tump_synth.Band` collects every note and, per instrument and pitch, stops a note where the same pitch is
picked again; the bass owns everything under C3 (checked: no guitar note below C3, no pitch in two instruments at once);
the opening is the guitar alone, soft, in its middle (A3 to C#5), the bass entering with the chalk run; the YA hit has
no low A of its own; the room's reflections are all upright and its echo is cut under 200 Hz (low reverb is mud).

| Round 5 measurement | Value |
|---|---|
| Energy under 150 Hz | 14 % (round 4: 34 %, 4b: 15 %) |
| Score roughness median / p90 | 0.082 / 0.253 (the match soundtrack: 0.082 / 0.217) |
| Film with the alley bed | -18.8 LUFS, true peak -2.9 dBTP |

## Round 4 (2026-10-23): no foley, real-sounding strings, the band counts

Watch: `r4-after.mp4` (with the alley bed and the kid's voice). Music alone: `r4-music-only.mp3` (the score and the
YA hit, nothing else; owner: "js an mp3 without the game, js the music").

| Owner on round 3 | Round 4 |
|---|---|
| "door opening sounds sucks" | no door foley: `sfx_film_tondo_door` deleted, its call gone from `TondoFilm.cs`. Each player's entrance is the band adding a part exactly on that door: door 1 the bass, door 2 the guitar, door 3 the bandurria, door 4 everything |
| "the running sfx sucks", "why even give his running some sfx" | the run has no sound: `sfx_film_tondo_chalk` deleted and uncalled. The music carries it |
| "MAIBA TAYA SOUND SUCKS" | the picture is a fist POINTING at each kid; nobody claps. `sfx_film_tondo_clap` deleted and uncalled. The band counts: a held tremolo breath, then one plucked bandurria note per syllable (E, F#, G#) doubled by the bass, each stronger; YA (T-05) is one decisive hit of the whole band on A, then it ducks for the kid's real "Taya!" (no tremolo answer) |
| "refine the music pa ang ijbol ng sound" | the cause, measured: round 2-3's strings were sine stacks, and the first physical strings were far too bright (harmonics 10-20 at -10 to -15 dB where a real harp's are -50 to -80; a C-E-G chord at roughness 0.53 against a real harp's 0.11). Now: Karplus-Strong strings plucked with a triangle (a real string's starting shape) and losing treble per note (3 kHz rings 0.12 s on nylon), a wooden body convolved in, a tremolo that re-picks a string still ringing, looser human timing. Exact tuning kept (within 0.5 cent) |

| Round 4 measurement | Value |
|---|---|
| Score roughness median / p90 | 0.070 / 0.262 (the match soundtrack, a professional track: 0.082 / 0.217) |
| YA hit roughness median / p90 | 0.191 / 0.238 |
| Sounding notes in A major | 1.00 |
| Film with the alley bed | -18.8 LUFS, true peak -2.2 dBTP (after 4b) |

**Round 4b** (owner, on round 4's first music mp3: "ang lakas ng music mo in some parts like it feels so deep and out
of tune"): two causes found. OUT OF TUNE: round 4 tuned the guitar's chords to pure intervals under an equal-tempered
bandurria, so the tune's C# sat 14 cents off the guitar's and the E7's seventh 31 cents flat; now every part is equal
tempered (one tuning). DEEP AND LOUD: 34 % of the score's energy sat under 150 Hz and the count's bass rivalled the rest;
the bass is quieter, doubles the count one octave down instead of two, the guitar is trimmed under 95 Hz and its body's
98 Hz mode lowered: 15 % now. The ending's swell is gentler and the compressor eased to 2:1, so the arc keeps its
dynamics: a hush of -25 dB just before the count, YA the loudest moment of the film at -14 dB.

Loudness note: the physical strings are spiky (a crest factor of 20.5 dB), so at the same peak the score measured
-24 LUFS. `tump_synth.compress` (a look-ahead peak compressor, 3:1 over -20 dB) brings the crest to 13.7 dB. Its first
version averaged the level and let every pluck attack through, so it made the score QUIETER; fixed and noted in the code.

The film now has two files of its own: `sfx_film_tondo_score` (T-01) and `sfx_film_tondo_taya` (T-05).

## Round 3 (2026-10-23): the story, the handoff into the match, nothing random

Watch: `r3-after.mp4` (with the alley bed). Per cue: `cues/*_r3.mp3`. The plan it follows: [spotting.md](spotting.md).

| Owner on round 2 | Round 3 |
|---|---|
| "005 random sound is weird", "may random shit na fucks it up" | the 0:05 sound was two bamboo clacks with nothing on screen to make them; cut, with the balde and the bandurria giggle. Rule: every sound has a reason on screen or a job in the music |
| of the chalk-run frame: the sound "rapes the shit" | T-02 is now what the picture shows: the kid's tsinelas slapping concrete every 0.2 s, a faint chalk scratch, a short scuff and plant for the skid; file level 0.55 |
| "refine the sound sa maiba taya even the sound they make" | T-04 is four kids clapping a little loose (0 to 22 ms apart), softer cracks, a palm body near the real takes; `TondoFilm.cs` lifts each clap 1.2 % (was 4 %, which put YA 12 % up like a toy synth), YA loudest. The chant itself is a human voice: a recording (docs/HUMAN.md), never a synth |
| "think of how a story has a start climax and end", "transition it to the actual game" | the match soundtrack (`ost_match.mp3`, which cuts in on the countdown's first tick) is in A major at about 150 bpm; the film is now in A on that 0.40 s pulse. Start: the guitar alone. Chalk: the groove starts, stops with the skid. Doors: one more part per door. Count: almost nothing, a swelling tremolo, the bass walking up E F# G#. YA: lands on A. Turn: F#m, E. End: E on every beat, growing, held on E7 into the fade, so the countdown and the match music in A answer it |
| "thoroughly analyze the material" | the film watched frame by frame; the handoff code read (`MapFilm.Sound`, `Hud.ShowCountdownTick`, `MusicDirector`); the match soundtrack measured for key and tempo |

New in the tools: strummed chords are tuned to pure intervals above their root (`tump_synth.JUST_CENTS`): the sting's
roughness p90 fell from 0.35 to 0.25. The match soundtrack itself measures roughness p90 0.22, the first real reference
for that number.

| Round 3 measurement | Value |
|---|---|
| Sounding notes in A major (score and sting) | 1.00 median, 1.00 p10 |
| Film with the alley bed | -18.9 LUFS, true peak -0.5 dBTP |
| Loudness by beat (0.5 s RMS) | start -21 to -25 dB, chalk -18, doors build to -17, huddle -21, count -20 to -14, YA -12 (the peak), turn -18, end builds to -14, then the fade |
| `MapFilmTests` | TondoRecording, TondoFullFilmBeats, TondoShortFilmBeats, TondoPlaysIntoTheRound pass |

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
| 2 | T-02, all | "005 random sound is weird", "may random shit na fucks it up", the chalk run "rapes the shit", "refine the sound sa maiba taya", "think of how a story has a start climax and end", "transition it to the actual game", "thoroughly analyze the material" | round 3: cut everything unmotivated; foley plays the picture; story arc; A major at the match's 150 bpm, ending on E into the match. **Rules for the skill:** spot the film first (watch it, write the beat sheet); every sound needs an on-screen reason or a musical job; score the scene's story arc; end a cutscene on the dominant of whatever plays next, at its tempo |
| 3 | T-02, T-03, T-04, music | "door opening sounds sucks", "the running sfx sucks", "why even give his running some sfx", "MAIBA TAYA SOUND SUCKS", "refine the music pa ang ijbol ng sound" | round 4: every foley cue of the film removed; the score marks doors and syllables; physical strings measured against a real harp. **Rules for the skill:** in a scored cutscene, foley only where it adds something music cannot (default: none); never foley an action the picture does not show (no claps for pointing); compare every generated instrument harmonic by harmonic with a real one before the owner hears it |
| 4 | T-01 | "ang lakas ng music mo in some parts like it feels so deep and out of tune" | round 4b: one tuning for every part; lows from 34 % to 15 % of the energy; gentler ending; the climax the loudest moment. **Rules for the skill:** never mix tuning systems across parts; keep the energy under 150 Hz near 15 % and give the low end to one instrument; check the loudness curve against the story (the climax must be the peak) |
| 4b | T-01 | "grabe ang deep pa ren and out of tune", "the start feels weirdly strum and deep", "it feels like its eating itself" | round 5: no pitch twice at once (a re-picked note stops the last; no unisons across parts; the bass alone under C3); upright room reflections; no low reverb; a light opening. **Rules for the skill:** model players, not oscillators (one string, one note); check for same-pitch overlaps across all parts before rendering; high-pass every reverb return |
