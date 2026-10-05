# The Arena's crowd: where every recording came from

Written by `tools/build_arena_crowd_from_recordings.py` from `tools/arena_crowd_sources.json`. Do not edit by hand.

Every crowd cue of the Arena (`sfx_arena_crowd_*` and `sfx_arena_chant_*`) is cut from the recordings below. All
were published by their authors under Creative Commons 0 1.0 (a public domain dedication), as stated on each
sound's own page when it was fetched on 2026-10-05. CC0 asks for no credit; the authors are named here anyway.
The downloads are not in the repository (`~/.cache/tump-audio/crowd-src/`): the script fetches them again and
checks each against its SHA-256. Freesound files are the site's high-quality MP3 previews, not the originals.

Nobody has listened to any cue yet. The cuts were chosen by measurement (spectrogram, loudness, beat, tonal and
speech measures, in `Logs/arena/audio/sources/`), which cannot recognise a club's song or a spoken word.

## Audition these first

Cues whose source span could not be cleared with confidence of music, a public address voice, one close voice or
an identifiable club chant:

### Highest risk

- `sfx_arena_crowd_bed_calm`: layer from source 9 (a small ground) may have single close voices (its speech measure is 0.12 to 0.17, the highest of the three); source 1 is the St. Pauli home crowd: the span analyses as diffuse (no steady beat, no tonal peaks, no speech-rate modulation), but a slow sung chant under a roar would not show
- `sfx_arena_crowd_bed_roar`: source 7 and 8 is the Atletico Mineiro crowd (Portuguese): the span analyses as a diffuse roar, but shouted words inside a roar would not show
- `sfx_arena_crowd_erupt_3`: source 8 is the Atletico Mineiro crowd (Portuguese): the span analyses as a diffuse roar, but shouted words inside a roar would not show
- `sfx_arena_crowd_ooh_3`: source 26 has referee whistles at 29.5 to 30.7 s and 31.5 s (cut round) and a fainter one near 33.75 s, which is inside this span at low level
- `sfx_arena_crowd_aww`: the studio take (15) is recorded very quietly (about 24 dB over its own noise): listen for hiss, and for single voices
- `sfx_arena_crowd_gasp`: one 2.5 s take multiplied: it may sound like the same shout five times, or carry a word
- `sfx_arena_crowd_gasp_2`: as sfx_arena_crowd_gasp
- `sfx_arena_chant_stomp`: built, not recorded: the stomps are slowed claps and may not read as feet on a stand
- `sfx_arena_chant_drums`: source 11 is a German club's supporters: the span is percussive with little energy in the voice band, but a chant sung quietly along with the drum would not show
- `sfx_arena_chant_horns`: the analysis cannot tell an air horn from a car horn or a vuvuzela: these are the two strongest steady-pitched blasts (118 to 122 s, 126 to 128 s)
- `sfx_arena_chant_ooh_hey`: AUDITION FIRST. Source 6 is a Brazilian crowd's held yell before a kick, which is customarily ended with a shouted word; the cut stops at 33.65 s, before the level drops and the shouting after it, and the cheer covers the cut, but only a listener can say that no word is left

### The rest (a real club's crowd, or a small studio group, that measured clean)

- `sfx_arena_crowd_bed_lively`: source 1 and 2 is the St. Pauli home crowd: the span analyses as diffuse (no steady beat, no tonal peaks, no speech-rate modulation), but a slow sung chant under a roar would not show
- `sfx_arena_crowd_bed_tension`: source 2 is the St. Pauli home crowd: the span analyses as diffuse (no steady beat, no tonal peaks, no speech-rate modulation), but a slow sung chant under a roar would not show
- `sfx_arena_crowd_erupt`: source 4 is the St. Pauli home crowd: the span analyses as diffuse (no steady beat, no tonal peaks, no speech-rate modulation), but a slow sung chant under a roar would not show
- `sfx_arena_crowd_erupt_2`: source 4 is the St. Pauli home crowd: the span analyses as diffuse (no steady beat, no tonal peaks, no speech-rate modulation), but a slow sung chant under a roar would not show
- `sfx_arena_crowd_cheer`: source 3 is the St. Pauli home crowd: the span analyses as diffuse (no steady beat, no tonal peaks, no speech-rate modulation), but a slow sung chant under a roar would not show
- `sfx_arena_crowd_cheer_2`: source 3 is the St. Pauli home crowd: the span analyses as diffuse (no steady beat, no tonal peaks, no speech-rate modulation), but a slow sung chant under a roar would not show
- `sfx_arena_crowd_cheer_3`: the studio take (17) is a few dozen close voices: single words or one voice may stand out of it
- `sfx_arena_crowd_save`: source 3 is the St. Pauli home crowd: the span analyses as diffuse (no steady beat, no tonal peaks, no speech-rate modulation), but a slow sung chant under a roar would not show
- `sfx_arena_crowd_ooh`: source 5 is the St. Pauli home crowd: the span analyses as diffuse (no steady beat, no tonal peaks, no speech-rate modulation), but a slow sung chant under a roar would not show
- `sfx_arena_crowd_ooh_2`: source 5 is the St. Pauli home crowd: the span analyses as diffuse (no steady beat, no tonal peaks, no speech-rate modulation), but a slow sung chant under a roar would not show
- `sfx_arena_crowd_aww_2`: the studio take (16) is a few dozen close voices
- `sfx_arena_crowd_aww_3`: the audience takes (19) are a small group; which are 'oooh' and which 'ahhh' is not known
- `sfx_arena_crowd_laugh`: source 21 is an old optical transfer and may sound dated; source 22 is a conducted audience
- `sfx_arena_crowd_laugh_2`: as sfx_arena_crowd_laugh
- `sfx_arena_chant_claps`: built, not recorded; source 3 is the St. Pauli home crowd: the span analyses as diffuse (no steady beat, no tonal peaks, no speech-rate modulation), but a slow sung chant under a roar would not show
- `sfx_arena_chant_claps_fast`: built, not recorded; source 3 is the St. Pauli home crowd: the span analyses as diffuse (no steady beat, no tonal peaks, no speech-rate modulation), but a slow sung chant under a roar would not show

## Still to be recorded by the team

The game's own word chants cannot be cut from another crowd. Their synthesised versions were rejected and deleted;
`ArenaCrowdAudio.Recorded` plays the cue in the last column in their place. To add one: record a group (ten or more
voices, several passes layered), save it as `Resources/Sfx/<name>.wav` (44.1 kHz mono 16 bit) and put `<name>` back
in `AudioCues.Live` with a `TrimDb` row of about -3.

| Cue | Words | Rhythm | Plays meanwhile |
|---|---|---|---|
| `sfx_arena_chant_tumbang_preso` | TUM-BANG! PRE-SO! | two beats on TUM-BANG, two on PRE-SO, at about 100 bpm, then clap clap, clap-clap-clap; three times, about 12 s | `sfx_arena_chant_claps` |
| `sfx_arena_chant_taya` | TA-YA! TA-YA! | two even beats, a beat's rest, six calls at about 96 bpm over a bass drum, about 9 s | `sfx_arena_chant_drums` |
| `sfx_arena_chant_tumba` | TUM-BA! TUM-BA! | two beats, seven calls speeding up from about 90 to 150 bpm, about 8 s | `sfx_arena_chant_claps_fast` |

## The cues

| Cue | Seconds | Recipe | Cut from (source: seconds from..to) | What it is |
|---|---|---|---|---|
| `sfx_arena_crowd_bed_calm` | 22.0 | loop | 27: 7.80..31.80; 9: 38.00..62.00; 1: 53.10..77.10 | the murmur: a thousand people talking (27), a small ground's ambience (9), a quieter passage of a full stadium (1) |
| `sfx_arena_crowd_bed_lively` | 18.0 | loop | 1: 288.70..308.70; 2: 186.30..206.30 | the same crowd awake: a loud passage of the general ambience (1) over a mood wave (2) |
| `sfx_arena_crowd_bed_roar` | 11.5 | loop | 7: 0.70..13.70; 8: 87.00..100.00 | everybody up: the first seconds of 60,000 yelling (7) with the roar after a goal (8) |
| `sfx_arena_crowd_bed_tension` | 16.0 | loop | 2: 218.40..236.40 | a held murmur: the quietest steady passage of the mood waves (2), gently low-passed. It is a hush, not a sung 'oooo' |
| `sfx_arena_crowd_bed_applause` | 14.0 | loop | 14: 43.90..59.90 | applause and cheering of a large crowd (14); the tonal measure shows whistles from the crowd in it |
| `sfx_arena_crowd_erupt` | 9.0 | layers | 4: 15.17..24.20 | a goal's eruption, second take of source 4 (at full size from its first sample) |
| `sfx_arena_crowd_erupt_2` | 9.5 | layers | 4: 0.10..9.60 | a goal's eruption, first take of source 4 (comes up over about two seconds) |
| `sfx_arena_crowd_erupt_3` | 9.1 | layers | 8: 85.95..95.00 | a goal at the Mineirao (8): from 25 dB down to full in half a second |
| `sfx_arena_crowd_cheer` | 5.0 | layers | 3: 0.32..5.35 | a swelling cheer, first take of source 3 |
| `sfx_arena_crowd_cheer_2` | 5.5 | layers | 3: 7.07..12.60 | a swelling cheer, second take of source 3 |
| `sfx_arena_crowd_cheer_3` | 8.7 | layers | 17: 9.00..15.90; 3: 14.60..20.20 | the studio group's recovery into cheers (17, with a little of the bowl) over a stadium cheer (3) |
| `sfx_arena_crowd_save` | 9.9 | layers | 3: 33.60..43.50 | the long swelling roar of source 3 (fifth take): a player saves themselves from the fall |
| `sfx_arena_crowd_ooh` | 3.7 | layers | 5: 45.52..49.20 | a chance missed, the short fifth take of source 5 |
| `sfx_arena_crowd_ooh_2` | 4.7 | layers | 5: 39.30..44.00 | a chance missed, fourth take of source 5 |
| `sfx_arena_crowd_ooh_3` | 6.4 | layers | 5: 0.10..6.50; 26: 31.70..35.60 | a chance missed, first take of source 5, over the 58,241 of France v USA after a foul (26) |
| `sfx_arena_crowd_aww` | 8.6 | layers | 5: 50.90..59.50; 15: 0.72..2.90; 15: 0.72..2.90 | a near miss in the stadium (5, sixth take) with the studio group's 'awww' (15) in the near rows |
| `sfx_arena_crowd_aww_2` | 5.9 | layers | 5: 16.40..22.20; 16: 1.72..5.60; 16: 1.72..5.60 | a near miss in the stadium (5, second take, from its reaction) with the studio group's groan (16) |
| `sfx_arena_crowd_aww_3` | 6.9 | layers | 5: 29.60..36.50; 19: 13.75..16.95; 19: 22.05..25.35; 19: 17.80..20.65 | a near miss in the stadium (5, third take, from its peak) with three of the audience's 'oooh' and 'ahhh' takes (19) |
| `sfx_arena_crowd_gasp` | 4.8 | layers | 18: 0.55..3.10; 18: 0.55..3.10; 18: 0.55..3.10; 18: 0.55..3.10; 18: 0.55..3.10 | the studio group's unison surprise shout (18), five copies at five pitches a few hundredths apart, in the bowl |
| `sfx_arena_crowd_gasp_2` | 4.7 | layers | 18: 0.55..3.10; 18: 0.55..3.10; 18: 0.55..3.10; 18: 0.55..3.10 | the same shout (18), four copies at other pitches and offsets |
| `sfx_arena_crowd_laugh` | 8.0 | layers | 21: 0.30..6.50; 22: 21.20..26.80 | a very large group laughing (21, an old film library transfer) over 300 people laughing on a conductor's cue (22) |
| `sfx_arena_crowd_laugh_2` | 7.8 | layers | 22: 2.00..8.00; 21: 0.30..5.00 | the conducted laugh's opening (22) over the large group (21, a little slower) |
| `sfx_arena_chant_stomp` | 11.7 | claps | 24: 0.10..65.00 | stomp-stomp-CLAP six times: single unison claps cut from source 24, played by 240 copies; a stomp is a clap slowed 2.5 times and low-passed |
| `sfx_arena_chant_claps` | 16.5 | claps | 24: 0.10..65.00; 3: 14.50..19.00 | clap-clap, clap-clap-clap four times (source 24's claps, 260 copies), answered by a stadium cheer (3) |
| `sfx_arena_chant_claps_fast` | 15.6 | claps | 24: 0.10..65.00; 3: 22.02..27.50 | clap-clap in pairs, speeding up over eight seconds (source 24's claps, 260 copies), ending in a stadium cheer (3) |
| `sfx_arena_chant_drums` | 13.2 | layers | 11: 1.00..14.20 | supporters' rhythmic clapping and drumming before the goal (11) |
| `sfx_arena_chant_horns` | 11.8 | layers | 25: 117.20..129.00 | supporters with horns (25): two long blasts with the supporters between them |
| `sfx_arena_chant_ooh_hey` | 11.1 | layers | 6: 28.60..33.65; 3: 22.02..28.40 | 60,000 holding a yell (6, its held vowel only) cut into a stadium cheer (3, fourth take) |

## The sources

### 1. Stadium general ambience, 29,546 fans

- Id: Freesound 829454
- Page: https://freesound.org/s/829454/
- File fetched: https://cdn.freesound.org/previews/829/829454_3625328-hq.mp3
- Author: itmightgetloud (Philipp Feit, Sound Of Sankt Pauli)
- Place: Millerntor Stadium, Hamburg, Germany
- Licence as stated: Creative Commons 0 1.0 (as stated on the sound's page) (the page was read before the download and states it)
- Fetched: 2026-10-05, 8483040 bytes, 372.1 s, MP3, 48000 Hz, 2 ch
- SHA-256: `00728388fe04840db4f8fd98e76d9140336bba313b27b82314d7a4951d27829c`
- Note: St. Pauli home crowd: club chants and goal music possible
- Used by: `sfx_arena_crowd_bed_calm` 53.10..77.10 s; `sfx_arena_crowd_bed_lively` 288.70..308.70 s

### 2. Mood waves, same crowd

- Id: Freesound 829456
- Page: https://freesound.org/s/829456/
- File fetched: https://cdn.freesound.org/previews/829/829456_3625328-hq.mp3
- Author: itmightgetloud (Philipp Feit, Sound Of Sankt Pauli)
- Place: Millerntor Stadium, Hamburg, Germany
- Licence as stated: Creative Commons 0 1.0 (as stated on the sound's page) (the page was read before the download and states it)
- Fetched: 2026-10-05, 5551824 bytes, 243.5 s, MP3, 48000 Hz, 2 ch
- SHA-256: `41b928d988ea51bfdc75376eae09ea5d98eeeead5b8fae82bf0beda2af870ca4`
- Note: St. Pauli home crowd: club chants possible
- Used by: `sfx_arena_crowd_bed_lively` 186.30..206.30 s; `sfx_arena_crowd_bed_tension` 218.40..236.40 s

### 3. Crowd excited, swelling roar

- Id: Freesound 829453
- Page: https://freesound.org/s/829453/
- File fetched: https://cdn.freesound.org/previews/829/829453_3625328-hq.mp3
- Author: itmightgetloud (Philipp Feit, Sound Of Sankt Pauli)
- Place: Millerntor Stadium, Hamburg, Germany
- Licence as stated: Creative Commons 0 1.0 (as stated on the sound's page) (the page was read before the download and states it)
- Fetched: 2026-10-05, 1334016 bytes, 63.1 s, MP3, 48000 Hz, 2 ch
- SHA-256: `71acd26f6bccdacb1c30d4cb299ff400917dec2856adc3c9414a8609d04acfdc`
- Note: St. Pauli home crowd
- Used by: `sfx_arena_crowd_cheer` 0.32..5.35 s; `sfx_arena_crowd_cheer_2` 7.07..12.60 s; `sfx_arena_crowd_cheer_3` 14.60..20.20 s; `sfx_arena_crowd_save` 33.60..43.50 s; `sfx_arena_chant_claps` 14.50..19.00 s; `sfx_arena_chant_claps_fast` 22.02..27.50 s; `sfx_arena_chant_ooh_hey` 22.02..28.40 s

### 4. Goal eruption

- Id: Freesound 829455
- Page: https://freesound.org/s/829455/
- File fetched: https://cdn.freesound.org/previews/829/829455_3625328-hq.mp3
- Author: itmightgetloud (Philipp Feit, Sound Of Sankt Pauli)
- Place: Millerntor Stadium, Hamburg, Germany
- Licence as stated: Creative Commons 0 1.0 (as stated on the sound's page) (the page was read before the download and states it)
- Fetched: 2026-10-05, 1508400 bytes, 68.5 s, MP3, 48000 Hz, 2 ch
- SHA-256: `0f5b0bf37d3c41b52fbfbba9400f3db904f94af50d68e385b2537db781fccdb2`
- Note: goal music or PA possible after the goal
- Used by: `sfx_arena_crowd_erupt` 15.17..24.20 s; `sfx_arena_crowd_erupt_2` 0.10..9.60 s

### 5. Near miss, chance missed reactions

- Id: Freesound 829452
- Page: https://freesound.org/s/829452/
- File fetched: https://cdn.freesound.org/previews/829/829452_3625328-hq.mp3
- Author: itmightgetloud (Philipp Feit, Sound Of Sankt Pauli)
- Place: Millerntor Stadium, Hamburg, Germany
- Licence as stated: Creative Commons 0 1.0 (as stated on the sound's page) (the page was read before the download and states it)
- Fetched: 2026-10-05, 1323768 bytes, 61.4 s, MP3, 48000 Hz, 2 ch
- SHA-256: `8056be66a39680ed78193d93320fd4627be30a9288039b3f4d42731cec9f60e1`
- Note: St. Pauli home crowd
- Used by: `sfx_arena_crowd_ooh` 45.52..49.20 s; `sfx_arena_crowd_ooh_2` 39.30..44.00 s; `sfx_arena_crowd_ooh_3` 0.10..6.50 s; `sfx_arena_crowd_aww` 50.90..59.50 s; `sfx_arena_crowd_aww_2` 16.40..22.20 s; `sfx_arena_crowd_aww_3` 29.60..36.50 s

### 6. 60,000 people yelling huuuuu

- Id: Freesound 612131
- Page: https://freesound.org/s/612131/
- File fetched: https://cdn.freesound.org/previews/612/612131_1661766-hq.mp3
- Author: Felix Blume
- Place: Mineirao stadium, Belo Horizonte, Brazil
- Licence as stated: Creative Commons 0 1.0 (as stated on the sound's page) (the page was read before the download and states it)
- Fetched: 2026-10-05, 1235424 bytes, 53.9 s, MP3, 48000 Hz, 2 ch
- SHA-256: `e3feaa9134f69db9ad01c13ba851e4f44d7ede0748bb3596e8ca82f58f1a51d6`
- Note: Atletico Mineiro crowd, Portuguese
- Used by: `sfx_arena_chant_ooh_hey` 28.60..33.65 s

### 7. 60,000 crowd yelling

- Id: Freesound 612046
- Page: https://freesound.org/s/612046/
- File fetched: https://cdn.freesound.org/previews/612/612046_1661766-hq.mp3
- Author: Felix Blume
- Place: Mineirao stadium, Belo Horizonte, Brazil
- Licence as stated: Creative Commons 0 1.0 (as stated on the sound's page) (the page was read before the download and states it)
- Fetched: 2026-10-05, 2339520 bytes, 98.9 s, MP3, 48000 Hz, 2 ch
- SHA-256: `ee7f0aad3f3fb7203cc09fe7ce38f143274cb6ef94dd20fb97bc0860f81c8c9f`
- Note: Atletico Mineiro crowd, Portuguese
- Used by: `sfx_arena_crowd_bed_roar` 0.70..13.70 s

### 8. Goals and crowd shouting, same match

- Id: Freesound 611816
- Page: https://freesound.org/s/611816/
- File fetched: https://cdn.freesound.org/previews/611/611816_1661766-hq.mp3
- Author: Felix Blume
- Place: Mineirao stadium, Belo Horizonte, Brazil
- Licence as stated: Creative Commons 0 1.0 (as stated on the sound's page) (the page was read before the download and states it)
- Fetched: 2026-10-05, 12624480 bytes, 535.4 s, MP3, 48000 Hz, 2 ch
- SHA-256: `cf126f84e6807a9c8ef00ec72cfb52f73bcc2e4f1b95c6b4e21f031e5443b2c3`
- Note: Atletico Mineiro crowd, Portuguese: chants likely
- Used by: `sfx_arena_crowd_bed_roar` 87.00..100.00 s; `sfx_arena_crowd_erupt_3` 85.95..95.00 s

### 9. Football match general ambience, smaller ground

- Id: Freesound 206003
- Page: https://freesound.org/s/206003/
- File fetched: https://cdn.freesound.org/previews/206/206003_878508-hq.mp3
- Author: habbis92
- Place: Stavanger, Norway
- Licence as stated: Creative Commons 0 1.0 (as stated on the sound's page) (the page was read before the download and states it)
- Fetched: 2026-10-05, 4021104 bytes, 174.9 s, MP3, 48000 Hz, 2 ch
- SHA-256: `4a8d6971b5ac59a14a59348fa8ce435f828337a434fed69aa5cf4bc5ad59d129`
- Used by: `sfx_arena_crowd_bed_calm` 38.00..62.00 s

### 10. Fans' reaction to a goal

- Id: Freesound 528799
- Page: https://freesound.org/s/528799/
- File fetched: https://cdn.freesound.org/previews/528/528799_11431915-hq.mp3
- Author: D.jones
- Place: not stated
- Licence as stated: Creative Commons 0 1.0 (as stated on the sound's page) (the page was read before the download and states it)
- Fetched: 2026-10-05, 2120965 bytes, 93.2 s, MP3, 44100 Hz, 2 ch
- SHA-256: `06b918aa8986ac38bd5f189f0038b08f4194760f36801f5e588093cd842a6235`
- Used by: nothing

### 11. Rhythmic clapping and drumming, whistle, then goal cheer

- Id: Freesound 189821
- Page: https://freesound.org/s/189821/
- File fetched: https://cdn.freesound.org/previews/189/189821_623488-hq.mp3
- Author: blaukreuz
- Place: Leipzig, Germany
- Licence as stated: Creative Commons 0 1.0 (as stated on the sound's page) (the page was read before the download and states it)
- Fetched: 2026-10-05, 1910880 bytes, 81.8 s, MP3, 48000 Hz, 2 ch
- SHA-256: `341515cd1e7c5347ea9654060437dea6597357bf67e133c7208e5e1d14c0febe`
- Note: drumming may carry a club chant
- Used by: `sfx_arena_chant_drums` 1.00..14.20 s

### 12. Supporters shouting and drums in a small stadium

- Id: Freesound 500250
- Page: https://freesound.org/s/500250/
- File fetched: https://cdn.freesound.org/previews/500/500250_1661766-hq.mp3
- Author: Felix Blume
- Place: Valparaiso, Chile
- Licence as stated: Creative Commons 0 1.0 (as stated on the sound's page) (the page was read before the download and states it)
- Fetched: 2026-10-05, 5192352 bytes, 221.7 s, MP3, 48000 Hz, 2 ch
- SHA-256: `07ecb2efbf67c69617531034916ec8278a458e14f99e450e860ac058df666c92`
- Note: Chilean club, Spanish: chants likely
- Used by: nothing

### 13. Big arena chanting and cheering

- Id: Freesound 353418
- Page: https://freesound.org/s/353418/
- File fetched: https://cdn.freesound.org/previews/353/353418_185417-hq.mp3
- Author: ckater
- Place: Hannover, Germany
- Licence as stated: Creative Commons 0 1.0 (as stated on the sound's page) (the page was read before the download and states it)
- Fetched: 2026-10-05, 1442578 bytes, 60.2 s, MP3, 44100 Hz, 2 ch
- SHA-256: `8b91133f42770b8eae553d08a929bc7844d2d51ed27392aa49b422fb4f30a8c0`
- Note: German club chanting
- Used by: nothing

### 14. Large crowd cheering and applause

- Id: Freesound 160493
- Page: https://freesound.org/s/160493/
- File fetched: https://cdn.freesound.org/previews/160/160493_341440-hq.mp3
- Author: Bansemer
- Place: not stated
- Licence as stated: Creative Commons 0 1.0 (as stated on the sound's page) (the page was read before the download and states it)
- Fetched: 2026-10-05, 1506576 bytes, 82.9 s, MP3, 48000 Hz, 2 ch
- SHA-256: `0c5028d21ec5f4aced402795c40ac99ec003794c79ced3255d97157ec44ad18a`
- Used by: `sfx_arena_crowd_bed_applause` 43.90..59.90 s

### 15. Crowd sigh of disappointment, awww

- Id: Freesound 763880
- Page: https://freesound.org/s/763880/
- File fetched: https://cdn.freesound.org/previews/763/763880_11744683-hq.mp3
- Author: ShangusBurger (Shane Vincent, GameSoundCon 2024 walla session)
- Place: GameSoundCon 2024, Los Angeles, USA
- Licence as stated: Creative Commons 0 1.0 (as stated on the sound's page) (the page was read before the download and states it)
- Fetched: 2026-10-05, 261888 bytes, 11.2 s, MP3, 48000 Hz, 2 ch
- SHA-256: `784e89178caf37d8171f0749a09f76e50fadf6a12fe66ccbede397617f1dba96`
- Note: studio group take, dry
- Used by: `sfx_arena_crowd_aww` 0.72..2.90 s; `sfx_arena_crowd_aww` 0.72..2.90 s

### 16. Crowd groan

- Id: Freesound 763808
- Page: https://freesound.org/s/763808/
- File fetched: https://cdn.freesound.org/previews/763/763808_11744683-hq.mp3
- Author: ShangusBurger (Shane Vincent, GameSoundCon 2024 walla session)
- Place: GameSoundCon 2024, Los Angeles, USA
- Licence as stated: Creative Commons 0 1.0 (as stated on the sound's page) (the page was read before the download and states it)
- Fetched: 2026-10-05, 349488 bytes, 15.0 s, MP3, 48000 Hz, 2 ch
- SHA-256: `4e347e304abe29e3206a4e5771d6086b5271aab035fbb0e5a67fa3cd3f37cf68`
- Note: studio group take, dry
- Used by: `sfx_arena_crowd_aww_2` 1.72..5.60 s; `sfx_arena_crowd_aww_2` 1.72..5.60 s

### 17. Excitement builds, let down, recovers into cheers

- Id: Freesound 763882
- Page: https://freesound.org/s/763882/
- File fetched: https://cdn.freesound.org/previews/763/763882_11744683-hq.mp3
- Author: ShangusBurger (Shane Vincent, GameSoundCon 2024 walla session)
- Place: GameSoundCon 2024, Los Angeles, USA
- Licence as stated: Creative Commons 0 1.0 (as stated on the sound's page) (the page was read before the download and states it)
- Fetched: 2026-10-05, 379728 bytes, 16.3 s, MP3, 48000 Hz, 2 ch
- SHA-256: `bb3792257a4c981cae4d96486dbd0f5bac2882522747cb929d42fb57fe4dec98`
- Note: studio group take, dry
- Used by: `sfx_arena_crowd_cheer_3` 9.00..15.90 s

### 18. Unison surprise shout

- Id: Freesound 763827
- Page: https://freesound.org/s/763827/
- File fetched: https://cdn.freesound.org/previews/763/763827_11744683-hq.mp3
- Author: ShangusBurger (Shane Vincent, GameSoundCon 2024 walla session)
- Place: GameSoundCon 2024, Los Angeles, USA
- Licence as stated: Creative Commons 0 1.0 (as stated on the sound's page) (the page was read before the download and states it)
- Fetched: 2026-10-05, 89904 bytes, 3.8 s, MP3, 48000 Hz, 2 ch
- SHA-256: `555d6d5b6e9e2fe8a937edcc7ee3e561f30a810543c5ea9da83b1f2332c71344`
- Note: studio group take, dry
- Used by: `sfx_arena_crowd_gasp` 0.55..3.10 s; `sfx_arena_crowd_gasp` 0.55..3.10 s; `sfx_arena_crowd_gasp` 0.55..3.10 s; `sfx_arena_crowd_gasp` 0.55..3.10 s; `sfx_arena_crowd_gasp` 0.55..3.10 s; `sfx_arena_crowd_gasp_2` 0.55..3.10 s; `sfx_arena_crowd_gasp_2` 0.55..3.10 s; `sfx_arena_crowd_gasp_2` 0.55..3.10 s; `sfx_arena_crowd_gasp_2` 0.55..3.10 s

### 19. Four audience oooh and ahhh

- Id: Freesound 264499
- Page: https://freesound.org/s/264499/
- File fetched: https://cdn.freesound.org/previews/264/264499_3890365-hq.mp3
- Author: noah0189
- Place: not stated
- Licence as stated: Creative Commons 0 1.0 (as stated on the sound's page) (the page was read before the download and states it)
- Fetched: 2026-10-05, 486048 bytes, 25.8 s, MP3, 48000 Hz, 2 ch
- SHA-256: `2602cb507d42d79b0d62873bb58e330873a03b5c544922b5cf13850d118858e1`
- Note: group take, dry
- Used by: `sfx_arena_crowd_aww_3` 13.75..16.95 s; `sfx_arena_crowd_aww_3` 22.05..25.35 s; `sfx_arena_crowd_aww_3` 17.80..20.65 s

### 20. Booing at a sport event

- Id: Freesound 557189
- Page: https://freesound.org/s/557189/
- File fetched: https://cdn.freesound.org/previews/557/557189_1535323-hq.mp3
- Author: Julien_Matthey
- Place: not stated
- Licence as stated: Creative Commons 0 1.0 (as stated on the sound's page) (the page was read before the download and states it)
- Fetched: 2026-10-05, 378432 bytes, 16.4 s, MP3, 48000 Hz, 2 ch
- SHA-256: `69ab76b867bc676f569cafae66543d576f1b99ab6208749e15011681da1c2ac3`
- Note: downloaded as approved; not used by any cue
- Used by: nothing

### 21. Very large group laughing

- Id: Freesound 480769
- Page: https://freesound.org/s/480769/
- File fetched: https://cdn.freesound.org/previews/480/480769_2524442-hq.mp3
- Author: craigsmith (USC film library transfers)
- Place: not stated
- Licence as stated: Creative Commons 0 1.0 (as stated on the sound's page) (the page was read before the download and states it)
- Fetched: 2026-10-05, 244416 bytes, 11.3 s, MP3, 48000 Hz, 1 ch
- SHA-256: `4c7297ec0853dc6be869e60b9589d85c89598a1d3280c01a5c4af126e5d1b753`
- Note: old film library transfer
- Used by: `sfx_arena_crowd_laugh` 0.30..6.50 s; `sfx_arena_crowd_laugh_2` 0.30..5.00 s

### 22. 300-person conducted laugh

- Id: Freesound 25296
- Page: https://freesound.org/s/25296/
- File fetched: https://cdn.freesound.org/previews/25/25296_173513-hq.mp3
- Author: freesound (La audiencia disponible)
- Place: Barcelona, Spain, 2006
- Licence as stated: Creative Commons 0 1.0 (as stated on the sound's page) (the page was read before the download and states it)
- Fetched: 2026-10-05, 1265760 bytes, 54.6 s, MP3, 48000 Hz, 2 ch
- SHA-256: `c65a0068c5fecae0f38829d580e75ef41a3b911242c0e15fc3ed9e42e3206626`
- Used by: `sfx_arena_crowd_laugh` 21.20..26.80 s; `sfx_arena_crowd_laugh_2` 2.00..8.00 s

### 23. Rhythmic crowd claps with cheers and whistles

- Id: Freesound 438396
- Page: https://freesound.org/s/438396/
- File fetched: https://cdn.freesound.org/previews/438/438396_2524442-hq.mp3
- Author: craigsmith (USC film library transfers)
- Place: not stated
- Licence as stated: Creative Commons 0 1.0 (as stated on the sound's page) (the page was read before the download and states it)
- Fetched: 2026-10-05, 1807440 bytes, 81.4 s, MP3, 48000 Hz, 1 ch
- SHA-256: `b784c1333b85239e4ff16aa2da74054fdf32282d3e87a16586cef4c78bf55f78`
- Note: old film library transfer
- Used by: nothing

### 24. Small group clapping in unison, clean

- Id: Freesound 197435
- Page: https://freesound.org/s/197435/
- File fetched: https://cdn.freesound.org/previews/197/197435_770707-hq.mp3
- Author: Yuval
- Place: not stated
- Licence as stated: Creative Commons 0 1.0 (as stated on the sound's page) (the page was read before the download and states it)
- Fetched: 2026-10-05, 1672464 bytes, 65.4 s, MP3, 48000 Hz, 2 ch
- SHA-256: `00b3862bb602460a47e089fd12b5657b39bb0d71355326a701b100b1ef9ced5a`
- Note: dry
- Used by: `sfx_arena_chant_stomp` 0.10..65.00 s; `sfx_arena_chant_claps` 0.10..65.00 s; `sfx_arena_chant_claps_fast` 0.10..65.00 s

### 25. Supporters with air horns, car horns and vuvuzelas

- Id: Freesound 99496
- Page: https://freesound.org/s/99496/
- File fetched: https://cdn.freesound.org/previews/99/99496_1535323-hq.mp3
- Author: Julien_Matthey
- Place: not stated
- Licence as stated: Creative Commons 0 1.0 (as stated on the sound's page) (the page was read before the download and states it)
- Fetched: 2026-10-05, 4261017 bytes, 185.0 s, MP3, 44100 Hz, 2 ch
- SHA-256: `bf65ee35308e6013e73d60e43126dbbce9fa7489d589bb9a3fdf872d7778cdc3`
- Note: car horns present
- Used by: `sfx_arena_chant_horns` 117.20..129.00 s

### 26. Missed chance, 58,241 spectators

- Id: Wikimedia Commons File:Match_amical_France-USA_-_58241_spectateurs_-_Action_ratée_sur_faute_21h38.ogg
- Page: https://commons.wikimedia.org/wiki/File:Match_amical_France-USA_-_58241_spectateurs_-_Action_ratée_sur_faute_21h38.ogg
- File fetched: https://upload.wikimedia.org/wikipedia/commons/1/17/Match_amical_France-USA_-_58241_spectateurs_-_Action_rat%C3%A9e_sur_faute_21h38.ogg
- Author: Romainbehar
- Place: France v USA friendly (stadium not stated here)
- Licence as stated: CC0 1.0 (as stated in the approved list; Commons file page)
- Fetched: 2026-10-05, 442618 bytes, 49.8 s, OGG, 48000 Hz, 1 ch
- SHA-256: `7c3086e81943dacaf2f207f4588f570cfa94b8214b40b4c9f01125a2aad5dc9a`
- Used by: `sfx_arena_crowd_ooh_3` 31.70..35.60 s

### 27. A thousand people chatting, medium distance

- Id: Wikimedia Commons File:360703_eguobyte_large-crowd-medium-distance-stereo.wav
- Page: https://commons.wikimedia.org/wiki/File:360703_eguobyte_large-crowd-medium-distance-stereo.wav
- File fetched: https://upload.wikimedia.org/wikipedia/commons/a/a9/360703_eguobyte_large-crowd-medium-distance-stereo.wav
- Author: eguobyte
- Place: not stated
- Licence as stated: CC0 1.0 (as stated in the approved list; Commons file page)
- Fetched: 2026-10-05, 10987470 bytes, 57.2 s, WAV, 48000 Hz, 2 ch
- SHA-256: `9821f013c7a752c809a1805aa9fc7a0ac17fd813b40e7d81ba3c286e2e8fd81f`
- Used by: `sfx_arena_crowd_bed_calm` 7.80..31.80 s

### 28. Referee whistle in a gymnasium

- Id: Wikimedia Commons File:218318_splicesound_referee-whistle-blow-gymnasium.wav
- Page: https://commons.wikimedia.org/wiki/File:218318_splicesound_referee-whistle-blow-gymnasium.wav
- File fetched: https://upload.wikimedia.org/wikipedia/commons/7/7d/218318_splicesound_referee-whistle-blow-gymnasium.wav
- Author: SpliceSound
- Place: not stated
- Licence as stated: CC0 1.0 (as stated in the approved list; Commons file page)
- Fetched: 2026-10-05, 488858 bytes, 3.3 s, WAV, 48000 Hz, 1 ch
- SHA-256: `6659d56633874aba1b03a1639df8019eef0fe57569ea98159e16d1273041c509`
- Note: downloaded as approved
- Used by: nothing
