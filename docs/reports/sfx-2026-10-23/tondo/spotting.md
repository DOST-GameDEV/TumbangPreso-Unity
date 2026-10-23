# Tondo film: the spotting sheet (what is on screen, what it should feel like, what the sound does)

Owner, 2026-10-23: *"thoroughly analyze the material ur making sfx and music for tapos dun mo iplan gagawin mo"*,
*"try to think abt how to transition it to the actual game? like think of how a story has a start climax and end
dapat may ganun din music"*, *"refine the sound sa maiba taya"*, and of the chalk run: the sound "rapes the shit".

Made by watching the recording frame by frame (`MapFilmTests.TondoRecording`, two frames a second) and reading
`TondoFilm.cs`, `MapFilm.Sound.cs`, `Hud.ShowCountdownTick` and `MusicDirector`.

## What comes after the film (the handoff)

The film's score fades over the return to the game (`MapFilm.Sound`: from `Handoff` to `End`, about 18.2 to 19.8 s).
Then the round's **3 · 2 · 1 · GO** countdown plays, and the **match soundtrack (`Resources/Music/ost_match.mp3`)
cuts in on the first tick** (`Hud.ShowCountdownTick`; the owner asked long ago for cuts, not crossfades).

Measured (`tump_synth.note_salience`, onset autocorrelation): the match soundtrack is in **A major** (key fit 0.81;
A, C# and E carry 47 % of its notes) at **about 150 bpm**, a beat every 0.40 s.

So the film must end as a **question the match answers**: the film in A major, on the match's 0.40 s pulse, ending on
**E** (the chord that pulls to A) during the YOU close-up. The countdown is the last stretch of suspense; the match
music arriving in A is the answer. The film's MAI · BA · TA · YA is already 0.40 s apart: the count is at the match's
own tempo, so the climax foreshadows the match.

## The story, beat by beat

| Time (s) | On screen | The story | Feeling | Music (rondalla in A major, 150 bpm) | Sound effects |
|---|---|---|---|---|---|
| 0.0 - 3.6 | looking up through laundry and fiesta banderitas at the sky; TONDO brushed on at 0.9; tilting down to the court and the can | START: the place | warm, wistful, an ordinary afternoon | the guitar alone, picking A slowly (half time); a soft bass A as the title lands; to E at 2.4 (a question: who comes?) | the alley's bed (the sparrows the owner likes) |
| 3.6 - 6.4 | a kid sprints the chalk line at the lens in tsinelas and skids to a stop (5.77), stands, nods | the first kid: the game is being set up | playful, quick | the groove starts: bass on the beat, guitar off it, A to D; a stop with the skid: one E strum, then a breath of silence | his running feet (every 0.2 s), a faint chalk scratch, the skid's scuff and plant |
| 6.4 - 10.2 | four doors, one each 0.9 s: each player steps out (YOU, AMIHAN, DANTE, ...) and walks to the can | RISING: the players arrive | anticipation, growing | it builds with each arrival: door 1 the bass, door 2 the guitar, door 3 the bandurria's tune, door 4 everything; A, F#m, D, E | each door: latch, a dry creak, the bump |
| 10.2 - 11.3 | wide: the four gather round the can | the huddle | expectant | the groove eases off; the bass settles on E | the bed |
| 11.3 - 12.95 | low, in the ring: fists over the lens, MAI · BA · TA · YA, pointing at each in turn | CLIMAX BUILD: the count | suspense, held breath | almost nothing: an E held by a bandurria tremolo swelling, the bass walking E, F#, G# under the claps | the four claps (four kids, loose, the last strongest) |
| 12.95 | YA lands on the taya | CLIMAX | the hit | the bass lands on A | the sting: one full A strum, then the bandurria answers (after the kid's real "Taya!") |
| 13.0 - 14.3 | the others laugh at the taya, who sags | comic relief | cheeky | two light up-strums of D and A | the kid's voice; the sting's tail |
| 14.3 - 15.6 | TAYA stamps on; the taya shakes it off and squares up | the turn | resolve | two firm strums, F#m then E | |
| 15.6 - 19.6 | each attacker's screen closes on themselves: YOU, ATTACKER; then the camera swings back to play | END: into the game | ready, leaning forward | E: the guitar on every beat getting stronger, a bandurria tremolo on B rising to E; it stops on E and rings as the score fades | (the countdown and the match music follow, in A) |

## Round 4 changes to this sheet

The run, the doors and the count now have NO sound effects (owner: "why even give his running some sfx",
"door opening sounds sucks", "MAIBA TAYA SOUND SUCKS"). The doors are marked by a part of the band entering on each
one; the count by a plucked note per syllable. Nobody claps in the picture: a fist points.

## Rules this sheet adds

- Every sound is motivated by the picture or has a job in the music; anything else is cut (round 2's bamboo and balde
  were the owner's "random sound" at 0:05).
- Foley plays what the picture shows: the chalk run is a kid's running feet, not two seconds of chalk.
- The music tells the scene's story: a start, a build, a climax on the count, a turn, and an ending that hands off to
  the match in the match's own key and tempo.
- The voices in "Maiba taya" are human: never synthesised (AGENTS.md). If the owner wants the kids chanting it, that is
  a recording (docs/HUMAN.md), not a synth.
