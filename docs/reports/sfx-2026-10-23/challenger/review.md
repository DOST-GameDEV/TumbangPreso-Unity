# C1: Home Before Dark

The owner approved the synced Tondo film on October 24, 2026 with "good" and requested that it be pushed into the game. This adopts C1 in place of the previous score and YA accent. It does not approve unheard gameplay effects or the other map films.

## Approved direction

Warm neighborhood nostalgia, then playful excitement. The owner's correction was "just one tempo and theme". One two-bar E-C#-B-A-B-A melody and one A-D guitar accompaniment keep the same 150 BPM pulse through the scene. The delivery grows firmer as the players arrive, thins around the count and voice, then fades into play. There are no separate run, door or count effects.

The actual film was inspected at 39 timestamped frames across every shot, against its cue log and current source. The sky and laundry establish the place, the chalk runner brings movement, the four entrances build company, YA at 12.9583 is the surprise, TAYA at 14.3 is the recovery and the YOU close-up returns attention to the player. Those beats shape dynamics without adding unrelated tunes for each camera cut.

## Production and exact adoption

The composition is preserved as `c1-composition.json` and `c1-composition.mid`. Surge XT 1.3.4 renders the two synthetic string parts through explicit MIDI events. A short A chord from that string vocabulary supplies YA. Preset versions and hashes are in `tools/tondo_challenger_sources.json`; the synth's official FAQ permits commercial use of generated output, including factory presets.

`tools/build_tondo_challenger.py` preserves the authoring and mix recipe. It discovers an existing synth or accepts `--plugin`, downloads only hash-checked presets and defaults to an audition directory. It never installs regenerated sound into Resources. The plugin's excitation noise can vary between runs, so regeneration is a new audition rather than a promise of identical PCM.

The two installed files are byte-identical to the owner's approved audition, with their existing Unity meta files retained:

- `sfx_film_tondo_score.wav`: SHA-256 `f739dcead9b5586cb9ce64e40a17183fe638aa76db27f8fc53dad11c3784b38a`, mono 44.1 kHz PCM24, 19.6 seconds.
- `sfx_film_tondo_taya.wav`: SHA-256 `7641c15b2ff449b61cade2657ee0ff7a16e43028a671965e4adba9f9f94ae1d2`, mono 44.1 kHz PCM24, 0.78 seconds.

The recorded child voice, five alley ambience beds, cue gains, score fades and game code retain their existing behavior. No paid generation service was used.

## Evidence

The approved encoded full-film preview measured -18.71 LUFS and -2.50 dBTP. The music-only WAV measured -18.80 LUFS and -3.83 dBTP. The lead's authored grid has no tempo deviation; rolled guitar dyads retain a 14 ms finger spread. The reference A4 measurement was 439.82 Hz. These are technical checks; the owner's playback approval supplies the taste decision.

The repository authoring script's composition-only mode reproduces the approved MIDI and timed events exactly. Existing synth discovery and both pinned preset downloads passed their hash checks. Both Python modules compile. The adopted skill at `.claude/skills/tump-sfx/SKILL.md` passes the skill validator.

Native adoption validation in Unity 6000.5.8f1 passed 3/3 with no failures or skips: `MapFilmTests.TondoFullFilmBeats`, `TondoShortFilmBeats` and `TondoPlaysIntoTheRound`. Fresh XML and the Editor log are under `Logs/tondo-c1-adoption-20261024/`. The log records successful AudioImporter runs for both WAVs. The guarded Editor exited successfully, preserving the named profile and restoring one shared Editor input preference.

The two existing audio meta files and three pre-existing dirty scenery/settings files retain their pre-run SHA-256 values. No player build was produced. A live audio capture is separate from the approved offline synced preview and remains open in TODO.
