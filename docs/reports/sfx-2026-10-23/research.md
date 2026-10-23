# SFX research, 2026-10-23: how to make TUMP sound good, starting with the Tondo film

Owner, 2026-10-23: *"this game has horrible sfx rn"*, *"we will focus on fixing tondo cutscene sfx muna and when its
likable we will turn it into a reproducible skill and theme"*, *"explore tools online and research heavily how to make
good sfx for our game and theme"*.

Only the owner's ears approve a sound. Everything measured here only keeps the mix safe and repeatable.

## 0. Why the current Tondo sounds are bad (diagnosis before research)

Every Tondo cue is built in `tools/synth_map_film_sfx.py` from band-passed white noise, sine partials and a
Karplus-Strong string, then normalised to 0.85 peak. That explains what the owner hears:

- **Noise pretending to be an object.** The clap is three bursts of 900-6000 Hz noise; the chalk is a train of
  identical noise grains; the door's creak is a sawtooth. Real objects have resonances, a material and a room.
  Band noise has none, so it reads as "static" rather than "hand", "chalk" or "wood".
- **Sines pretending to be instruments.** The lullaby's music box and the taya glockenspiel are four sine
  partials with exponential decays. There is no strike transient and no body, so they sound thin and electronic.
- **Every cue normalised to the same peak.** A 0.3 s clap and a 20 s score both leave the generator at 0.85
  peak, so loudness between cues is accidental and only the call-site volume tames it.
- **No variation.** The four claps are the same file at four pitches; the four doors are one file at four pitches.

## 1. How good party games handle SFX

Sources: [Overwatch: The Elusive Goal, Play by Sound (GDC 2016, Scott Lawlor and Tomas Neumann)](https://www.gdcvault.com/play/1023317/Overwatch-The-Elusive-Goal-Play),
plus widely published postmortems and interviews on Fall Guys (Mediatonic), Brawl Stars (Supercell), Splatoon
(Nintendo) and Rocket League (Psyonix). The common practices, and what each means for TUMP:

| Practice | What the games do | What it means for TUMP |
|---|---|---|
| One sound per event (readability) | Overwatch built its whole mix around "play by sound": every gameplay event gets a distinct, recognisable signature, and less important sounds are culled or ducked so the important one is always heard. Brawl Stars keeps cues very short and distinct because the phone speaker is small and matches are busy. | Each Tondo beat gets one signature (chalk, door, clap, taya). Nothing else plays on the same frame except the score. The kid's "Taya!" and the sting share a frame on purpose, so the sting must leave the voice's 300-3000 Hz room. |
| Layering: transient, body, tail | Professional cues stack a short attack (click, snap, tick), a body in the object's own material, and a short tail or room. Fall Guys' bouncy, rubbery cues are layered real foley plus a pitched "toy" layer. | Already the house rule in `docs/HERO_KIT_METHOD.md` section 5 (transient / body / tail). The Tondo cues lacked a REAL body. Use a recording for the body and keep synthesis to the sweetener. |
| Cartoon exaggeration | Fall Guys and Splatoon pitch foley up, shorten it, and add a tuned element (a boing, a marimba tick, a squelch) so it reads as a toy. Splatoon's ink sounds are real liquid foley pushed into a playful register. | Real tsinelas, chalk, tin can and claps, recorded dry, then: shorter, a little higher, a tight small-room tail, and a tuned toy layer (glockenspiel, xylophone, marimba) on the important ones. |
| Music and SFX in one key | Party games tune their stingers and UI ticks to the score's key so hits feel musical. | The Tondo score is in C major pentatonic at 100 bpm. Tuned cues (taya sting, door tines) stay in C major. |
| Ducking under the important event | Overwatch and Rocket League duck the music and ambient beds when a priority cue fires. | The film already fades the score with the film. Round 1 writes a quiet pocket into the score itself on the claps and the YA hit, instead of adding runtime ducking. |
| Variation | Round-robin samples plus small random pitch (about plus or minus 3 to 5 percent) and volume stop the "machine gun" effect. | Claps and doors already shift pitch per seat. Round 1 also builds each clap from a different mix of real claps, so the four sound like four hands, not one file. |
| Loudness targets | Integrated loudness is measured with ITU-R BS.1770 / [EBU R 128](https://tech.ebu.ch/docs/r/r128.pdf) (LUFS) and true peak (dBTP). Sony's ASWG-R001 guideline asks for about -24 LUFS integrated on home console and -18 LUFS on portable, with true peak at or below -1 dBTP. Mobile and PC party games sit nearer -16 to -18 LUFS. | Film plus ambience must stay under 0 dBFS (handoff rule). Working target for the film mix: about -18 LUFS integrated, true peak at or below -1 dBTP, measured with ffmpeg's [ebur128](https://ffmpeg.org/ffmpeg-filters.html#ebur128) filter. One-shots are judged by their short-term loudness against the score at their call time, not by a shared peak. |

### 1.1 What the people who made these games actually said (read 2026-10-23)

Owner, 2026-10-23: *"if ur gonna do research watch real games na may good sfx"*. The agent cannot hear audio, so
this pass reads what the games' own sound designers and composers said about how they built their sound:

- **Untitled Goose Game** (sound by Em Halberstadt, [A Sound Effect interview](https://www.asoundeffect.com/untitled-goose-game-sound/);
  [House House Q&A, Game Developer](https://www.gamedeveloper.com/design/behind-the-honk-an-i-untitled-goose-game-i-q-a)).
  The closest match to TUMP: a simple, pleasant, toy-like world. She recorded every prop herself, in her house and
  backyard, picking up, dropping and dragging each one on three surfaces. The aim was sounds "easy to listen to and
  understated", so the one loud gag (the honk) is funnier against "the subtlety and niceness of all the prop
  sounds". Each prop shares a grab / release / impact set. When she muffled a crash to be realistic, the developers
  asked for more cartoon instead, and it worked better. The old trailer used royalty-free library placeholders and
  every one was replaced. The music is one instrument (solo piano), chopped into two-beat pieces so it can land
  exactly on what the player does.
  **For TUMP:** real recordings of the alley's own objects, dry and pleasant; one loud signature per film (the taya
  landing), everything else understated; music that lands on the action.
- **Splatoon** (sound director Toru Minegishi, [Nintendo Ask the Developer Vol. 7](https://www.nintendo.com/us/whatsnew/ask-the-developer-vol-7-splatoon-3-part-1/)).
  The series' sound started from the producer's question "what kind of band is playing this music?", treating the
  battle music as songs by bands inside the Inkling world, and that "led us to create a unique sound for the
  series". The first game's music was "fast, playful and summery... using just a few notes".
  **For TUMP:** decide who is playing in the world. In Tondo the band is the kids and the street: a toy
  glockenspiel and xylophone, a cheap plucked guitar, claps, sticks on wood, a tin can, a plastic bucket. Every
  sound in the film should be something that could exist in that eskinita. That one decision is what makes the
  cues belong together (section 4).
- **Fall Guys** (composers Jukio Kallio and Daniel Hagstrom, [Game Grooves interview](https://gamegrooves.com/gazette/fall-guys-interview)).
  They built the first track specifically to set "a big enough sound palette that was interesting to use for
  everything that comes after it", and drew the sound from the characters: "wobbly and bouncy and goofy... never too
  serious, yet serious enough".
  **For TUMP:** pick the palette first, then make every cue from it. The palette comes from the picture: blocky,
  warm, toon, childhood.
- **Overwatch** ([GDC 2016, Play by Sound](https://www.gdcvault.com/play/1023317/Overwatch-The-Elusive-Goal-Play)):
  the mix is built so the important event is always heard and lesser sounds get out of its way.
  **For TUMP:** the loudness ladder and spectral lanes in section 4.

Not used: an official Brawl Stars audio fan kit exists ([Supercell fan kit](https://fankit.supercell.com/d/YvtsWV4pUQVm/audio)),
which could be measured as a real party game's loudness and length reference. Downloading it is outside this
task's CC0 sourcing, so it waits for the owner's OK.

## 2. Our theme: a Filipino street game in a Tondo eskinita

The picture is blocky, toon-shaded and warm. The sound should be **real street materials played like toys**.

- **The palette of objects.** The tin can (lata): a bright, short, slightly hollow "tink-tonk". Rubber tsinelas on
  concrete: a flat slap with a squeak of rubber. Chalk on rough concrete: a dry grainy scrape, not a blackboard
  squeal. Wooden and corrugated-iron house fronts: knocks, latches, short creaks. Kids' hands: small, bright claps.
- **The palette of instruments.** A toy glockenspiel and xylophone (childhood, the music box under the laundry),
  a marimba and a log drum for warm wooden lows, claves and a shaker for the street rhythm, and plucked strings in the
  role of the rondalla's bandurria and guitar. The Filipino flavour comes from plucked tremolo strings and
  bamboo-and-wood percussion more than from any single exotic instrument.
- **The barangay ambience** is already in the map: `Art/audio/ambience/alley_bed_*` (neighbourhood, street, kids,
  sparrows, wind) and the one-shots (gate, dishes, bell, horns, chickens, dogs), all CC0 recordings logged in
  `tools/eskinita_life_sfx_sources.json`. The film plays over that bed, so film cues must sit ABOVE a busy bed: short,
  bright transients that the bed does not mask, and no low rumble that adds mud to it.
- **How to make it cartoony and punchy.** Trim to the transient plus 100 to 400 ms. Raise pitch 1 to 3 semitones
  for small objects (kids' hands, a small door). Roll off below about 120 Hz on one-shots so the bed and score keep the
  low end. Add a short, bright room (about 0.3 to 0.6 s) instead of a long reverb. Put a tuned toy layer under the
  beats that must feel like a "moment" (the count landing).
- **Voices.** No synthesised or imitated human voice (AGENTS.md, human-voice policy). The kid's "Taya!" at the count
  is a real recording already in the project (`sfx_life_kid_taya_1`, picked up from `SidewalkLife.KidTaya[0]`), which
  `docs/MAP_FILM_METHOD.md` rule 4 explicitly allows. It stays as is.

## 3. Tools, and what each licence allows

| Tool / source | Kind | Licence and what it allows | Verdict for TUMP |
|---|---|---|---|
| [Freesound](https://freesound.org/help/faq/) | Recordings | Per sound: CC0 (no conditions), CC-BY (credit required), CC-BY-NC (no commercial use, excluded). Previews download without an account. | **Use, CC0 only by default.** Each sound's own page is read and must state CC0, exactly as `tools/build_eskinita_life_sfx.py` does. CC-BY only if logged and credited in `CreditsContent`. |
| [VSCO 2 Community Edition](https://github.com/sgossner/VSCO-2-CE) | Multisampled orchestral and percussion library | CC0 1.0 (stated in the repository's licence). | **Use.** Glockenspiel, xylophone, marimba, harp, claves, log drums, guiro, triangle, tambourine: real recorded notes replace the sine bells and Karplus-Strong strings in the score and the taya sting. |
| [Kenney audio packs](https://kenney.nl/assets/impact-sounds) | Game SFX packs | CC0. | Use as transient support (already in `docs/Asset_Sourcing.md` 5.1). The owner rejected some Kenney-based cues by ear (section 5.4), so not as the whole cue. |
| [Sonniss GameAudioGDC bundles](https://sonniss.com/gameaudiogdc/) | Professional recordings | Free royalty-free commercial use, no credit; raw files must not be redistributed, so they cannot live in this public repo. | Possible later for designed layers, compiled-only. Needs a large download; not used in round 1. |
| [jsfxr / sfxr.me](https://sfxr.me/) | Retro synth generator | Generated output is yours to use. | Not the TUMP look: chiptune bleeps clash with the toy-real palette. Useful only for UI blips. |
| [ChipTone](https://www.leshylabs.com/apps/sfMaker/) | Retro synth generator | Generated output free to use. | Same as jsfxr. |
| AI SFX generators, e.g. [ElevenLabs Sound Effects](https://www.elevenlabs.io/sound-effects) | Text to sound | Commercial licence only on paid plans ([pricing](https://elevenlabs.io/pricing), [terms](https://elevenlabs.io/terms-of-use)); the free plan needs attribution and is not licensed for commercial use. | **Not used**: it needs a sign-up and a paid plan, which needs the owner's OK. Also harder to reproduce byte for byte. |
| [Audacity](https://www.audacityteam.org/) | Editor | GPL editor; the audio you make is yours. | Fine for the owner to audition and trim by hand. The pipeline stays scripted so it is reproducible. |
| [Vital](https://vital.audio/), [Surge XT](https://surge-synthesizer.github.io/) | Synths | Free synths; your rendered audio is yours. | Optional for future tonal sweeteners. Not needed while VSCO covers the toy instruments. |
| [pedalboard](https://github.com/spotify/pedalboard) (Spotify), pyo | Python DSP | GPL-3 (pedalboard); output is yours. | Not installed. numpy/scipy already do the filters, compression and room the cues need, and keep the build dependency-free. |
| [ffmpeg ebur128 / loudnorm](https://ffmpeg.org/ffmpeg-filters.html#loudnorm), [pyloudnorm](https://github.com/csteinmetz1/pyloudnorm) | Loudness measurement | LGPL/GPL tool, MIT library. | **ffmpeg ebur128** is installed and used for every measurement in this report. |

No paid service, account or sign-up was used. Every licensed source used is logged with its page, author, licence
and SHA-256 in `tools/tondo_film_sfx_sources.json`.

## 4. Cohesion: why the cues fight each other now, and the rules that make them one sound

Owner, 2026-10-23: *"thoroughly think abt how to make sfx cohesive kasi rn at current state its js noise and a bunch
of sounds taht dont have a theme and dont work well together or dont support each other"*.

What breaks cohesion today, cue by cue against the score:

- **Five different worlds.** The score is sines and plucks in a synthetic reverb; the clap and chalk are white noise
  with their own little noise reverb; the door is a sawtooth in a third reverb; the kid's voice and the map bed are
  real recordings in a real street. Each lives in its own space, so nothing sounds like it happens in the same alley.
- **No shared pitch.** The door creak sweeps 380-630 Hz at random against a C major score. The clap's pitch steps
  (+4 % per syllable) are not notes. Only the taya sting is in key.
- **No shared time.** The cues land on film beats, but the score's accents were placed independently, so a door
  can land between two strums and the YA clap sits on top of a kick and a chord and a glock run and the voice.
- **Everything full range.** Every cue covers 200 Hz to 10 kHz, the same band the score and the bed use, so each
  new sound masks the last instead of sitting beside it.

The rules every Tondo cue now follows (and that the TUMP SFX skill will inherit):

1. **One band, and it lives in the alley (the Splatoon question).** Every sound is something the kids of the
   eskinita could play or touch. The music and the tonal sweeteners all come from the same toy band:
   glockenspiel, xylophone, marimba, harp (as the rondalla's plucked strings), claves, log drum, triangle. No sine
   bells, no synth pads leading. Foley comes only from real recordings of the alley's own materials: hands, chalk,
   wood, tin, rubber.
2. **One key and one grid.** C major pentatonic (C D E G A) at 100 bpm. Every tuned element of a cue is a note in
   that scale. The score's accents are moved onto the cue times from the film's own `cues.txt`, so the doors land
   on the groove's downbeats and the claps land in the score's gaps.
3. **One motif.** The lata motif is three notes, G-E-C rising to C as a "do-mi-sol-do" (C5 E5 G5 C6). It is the
   lullaby's opening shape, each door's tine plays the next note of it, and the taya sting completes it. The ear hears
   the film resolving one idea, not five unrelated sounds.
4. **One room.** Every cue and the score go through the same short room impulse (a small concrete alley, about 0.5 s,
   darkened above 6 kHz). Same space means the clap, the door and the glockenspiel are heard in one place.
5. **One loudness ladder, one spectral lane each.** The score is the bed and is high-passed out of the way under
   each cue. Each cue owns a lane: chalk is a high grainy texture (2-8 kHz), the door a mid wooden knock (200-2 kHz)
   plus a tine on top, the claps a bright snap (1-6 kHz) with the score emptied on those hits, and the taya sting a
   wide "moment" that leaves 300-3000 Hz open for the kid's real "Taya!". Below about 120 Hz belongs only to the
   score's marimba and log drum, so the map's bed and the cues do not muddy it.
6. **Foley in the cue, the tuned layer in the score, on the same frame.** Each foley event gets one tiny tuned
   answer from the band (a glockenspiel tine on each door, a woodblock-and-xylophone step on each clap climbing
   C D E to G, a xylophone flick on the chalk's skid). That layer is written into the SCORE at the cue's exact film
   time rather than baked into the cue file, because the film plays the door and clap files at per-seat pitches
   (`TondoFilm.cs:311`, `:363`), which would push a baked-in note out of key. Real material from the cue, musical
   pitch from the score: that is the glue.
7. **Variation inside the theme.** Each repeat is a different recording or a different blend; tuned answers move by
   scale steps, never by arbitrary percent.

## 5. The method this report leads to: generate, learned from references

Owner, 2026-10-23: *"i wanted u to learn how to use a synthesizer or audio generation shit for our game tump"*, then
chose **synth, learned from refs**: every shipped sound is generated by code, and the CC0 recordings above
(Freesound, VSCO 2 CE) are only MEASURED. Nothing recorded ships. This is analysis and resynthesis, the
physical-modelling approach of procedural-audio work: measure what makes a real object sound like itself, then build
it from those numbers.

1. **Measure** (`tools/tump_synth.py`): `modal` finds each struck or plucked object's partials (frequency, level,
   decay rate in dB/s); `texture` finds a noisy event's sixth-octave spectrum, attack, decay and grain rate; `creak`
   finds a hinge's stick-slip pulse rate and body resonances. The results are committed in
   `tools/tump_sfx_profiles.json`, so a rebuild needs neither the network nor the references.
   What the numbers showed: the glockenspiel's partials sit at about 2.94, 3.52 and 5.54 times the fundamental,
   which rings at 8 dB/s; the marimba is tuned 1:4:10; the xylophone to its 3rd harmonic; the coffee can is
   inharmonic (1.0, 1.49, 2.38, then a cluster near 7 times); a real hinge squeals at about 600 Hz.
2. **Generate**: a modal bank under a mallet model (soft = dark, hard = bright) for the toy band and the can;
   measured-spectrum noise bursts plus a 1.5 ms crack and the palms' resonances for claps; grains on the chalk's
   spectrum at its measured rate; stick-slip through the measured body for the creak.
3. **Check by eye before the owner's ear**: a spectrogram of each generated cue beside its reference
   (round 1 caught a bell-like door bump, a clap singing a steady 3 kHz tone, and chalk that was a wall of noise,
   all fixed before the owner heard anything).
4. **Loudness by measurement, approval by ear**: the whole film rendered with the map's alley bed, integrated
   loudness and true peak by ffmpeg ebur128; under 0 dBFS true peak is the hard rule.
5. **Change only what the owner names.** Every cue has an ID (T-01 to T-07) in `tondo/review.md`.

Also set up for later rounds (not used in round 1): [Surge XT](https://surge-synthesizer.github.io/) 1.3.4 (free,
open source; output is ours), downloaded from its official GitHub release into `%LOCALAPPDATA%\TUMP-audio-tools\`
outside the repo, renders headlessly from Python through [pedalboard](https://github.com/spotify/pedalboard)
(installed with pip), with its 599 parameters scriptable. That is the "hangin" (air) instrument and the route to
music later.
