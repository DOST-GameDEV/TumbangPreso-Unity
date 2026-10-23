---
name: tump-sfx
description: Compose original music and synthesize sound effects for TumbangPreso from the actual scene, gameplay and the owner's listening feedback. Use for TUMP cues, themes, music transitions and sound revisions.
---

# TUMP music and sound

Owner-approved music benchmark, 2026-10-24: Home Before Dark, C1 for the Tondo film. The owner heard the synced film, said it was good and requested integration. Its one-tempo, one-theme direction and MIDI-to-Surge workflow are adopted. Other films, gameplay effects and new cues still need their own scene analysis and listening approval.

## Start with the scene

Read current source and inspect the actual supplied film or gameplay. Establish exact timing, perspective, physical action, emotional turn and what comes immediately afterward. Source comments can be stale; use the captured picture to check them. Music and effects must serve those specific things.

Express the intended feeling in plain language. The owner does not know music theory: own the technical choices and ask about feelings or audible qualities when useful. Translate "sunken," "too much" or "the opening feels weird" into a focused hypothesis; do not insist the owner diagnose frequencies or chords.

Ask before actions that spend external generation credits, require signup or use paid services. Never reset usage. Reuse installed tools before downloading more; Windows packaged apps may resolve LOCALAPPDATA inside their package cache. Keep repository and other contributors' assets intact while auditioning a challenger.

## Compose music deliberately

Write a recognizable melodic idea before adding a production stack. Develop it across the scene: a return can change rhythm, register, harmony, articulation or confidence. Keep space for the environment, voices and the player's attention. Identify the emotional job of each new part; remove parts without a useful job.

Owner's current direction: keep one tempo and one recognizable theme. Do not change tunes or invent separate musical miniatures for each camera cut. Carry the same pulse and melodic identity through the scene; use arrangement, articulation and dynamics to shape its emotional arc. The Tondo challenger uses one 150 BPM pulse and one recurring two-bar melody.

Choose culturally relevant musical language with care. Filipino identity can come from phrasing, accompaniment and musical context as well as instrument color. A generic pentatonic pattern or decorative bamboo sound does not establish it. For the present Tondo direction, investigate intimate harana/rondalla-like strings and neighborhood warmth rather than treating every Philippine setting as the same ensemble.

Keep pitches, harmony and voicings intentional. Preserve an editable composition as MIDI or timed note events. Use phrase dynamics, plausible fingering, articulation and purposeful timing deviations. Random timing or velocity jitter alone is not musical performance. Do not claim generated music was performed by a human.

Establish the instrument color with a short phrase before expanding. Prefer an established synth or physical model when it helps; an impressive synthesis implementation is not evidence that its output feels good. Preserve patch state, versions and sound-source provenance. Render generated instruments only; recorded examples may inform analysis but are not shipped sample layers under the current TUMP policy.

For the present TUMP taste direction, avoid bells, glockenspiel, triangle, music-box timbres, xylophone and marimba. Never synthesize human voices. Preserve approved recorded voices and ambience unless the owner changes their scope.

## Design effects around their function

Identify what information the player needs: contact, material, motion, success, danger or a character's intent. Define a clear onset, useful body and appropriate tail; audition in context at the likely repetition rate. Every layer must help that information or the artistic intention.

Music can already supply an accent. Do not add literal foley to every movement, cut or caption. In Tondo, the owner rejected running, door and counting foley. The count is pointing, not clapping. Preserve that decision unless reopened.

Design a family through related material and behavior, not by applying the same noise recipe to every hero or map. Preserve distinct character identities and keep simultaneous gameplay cues readable.

## Render, verify and listen

Render through the real game's timing, volume, fades, voice and ambience. Also deliver music alone when it helps distinguish writing from the mix. Match loudness and channel layout for meaningful comparisons. Check the beginning, reveal, quiet moments, tails and transition into play.

Use loudness, peaks, spectral balance, mono compatibility and transient checks to catch technical faults. They do not establish emotion, cultural character, realism or the owner's preference. Do not infer the cause of an audible complaint from a single metric. A dark tone is not automatically out of tune; doubled pitches are not automatically wrong. Isolate parts and check phase, tuning, register and masking where the evidence points.

Avoid narrow fixed resonances or heavy low-end buildup when trying to add body. Keep room tails short enough for the scene and preserve voice intelligibility. Tune gain and peak headroom against the actual combined mix. Current Tondo trial target is approximately -18 LUFS with the bed and at most -1 dBTP; this is a scene target, not a universal loudness rule for every sound.

Label the new candidate clearly and deliver directly in chat. Record the owner's exact feedback, what changed and whether the result was approved. Revise the named problem while preserving working choices. Stop speculative alternate renders once the concrete candidate is ready to hear; the next useful evidence is the owner's reaction.

Only integrate an audition into shipping resources once the owner selects it. Do not replace the incumbent builder or established skill just because a render passed technical checks. Keep useful source, patch state and current evidence; remove task-owned duplicate downloads, obsolete renders and idle helpers after verifying ownership.


## Approved Tondo implementation

`tools/build_tondo_challenger.py` preserves the composition and rendering recipe. Run `py -3 tools/build_tondo_challenger.py --composition-only --out <audition-folder>` to inspect the MIDI without synthesis. Without `--composition-only`, it uses an existing Surge XT VST3, fetches two hash-checked official presets into its audition cache and renders clips plus the synced film. Pass `--plugin <inner-vst3-binary>` or set `TUMP_SURGE_VST3` when automatic discovery does not find the installed copy. Requires numpy, pedalboard, mido and ffmpeg.

The renderer defaults to `docs/reports/sfx-2026-10-23/challenger/regenerated/` and never installs into Resources. An engine's excitation noise can vary between renders; the exact approved shipped WAVs are identified by SHA-256 in `tools/tondo_challenger_sources.json`. Treat regenerated sound as an audition until heard. Preserve existing Unity meta files when installing selected audio.

The synced preview requires `Logs/map-films/Eskinita/video/` from a real recording. If those frames are absent, use the repository's guarded Unity recording workflow and `MapFilmTests.TondoRecording`; do not substitute unrelated footage.

Current implementation: one two-bar E-C#-B-A-B-A melody at 150 BPM, one repeating A-D accompaniment, two synthetic string colors, dynamics across the film and a brief A chord at YA. The score leaves space for the real child recording and fades into play. Do not restore separate run, door or count effects. Technical measurements and owner feedback are in `docs/reports/sfx-2026-10-23/challenger/review.md`.
