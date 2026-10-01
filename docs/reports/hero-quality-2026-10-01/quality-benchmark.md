# Deeper benchmark against the finalized reference work

This is an evidence-backed comparison in progress, not a declaration that the
new work is better. The owner explicitly requests a higher quality bar and has
reopened all prior SFX, including Paete/Phaister sounds. Their other finalized
work remains protected pending any specific new exception.

## Actual authoring pipeline inspected

The reference quality did not come from an engine switch or one image prompt.
Paete props and Phaister doll geometry are authored through Python mesh/GLB
builders, shared voxel helpers, named moving nodes, deliberate palettes and
material-specific shaders. The inspected prop builders use standard Python
geometry/serialization helpers, not a required Blender authoring session.
Runtime C# poses those named parts. Body animation uses authored pose curves;
intro authoring writes the character-specific body/shot data consumed by Unity.
The body, first-person action, prop, field, camera and accepted live state are
separate authored components with explicit handoff responsibilities.

The strongest improvements were specific revisions backed by native frames:
root cords with origins/destinations instead of ornamental coils, a three-heave
emergence instead of scaling a finished tree, glow inside openings instead of
paint on the surface, and an ending that holds the doll's stare rather than
rushing through another unrelated action. Detail density alone did not solve
these failures. The repeated human critique is part of what produced the result.

## Do not mistake historical comments for the current performance

Current Phaister source contains the later six-second ending, with iris closure
from5.1seconds and no cutscene drop/body-turn. Its older v7 header and early plan
still describe a6.4second drop/landing sequence. The final sampled constants and
actual render are authoritative for this comparison. Likewise, Paete's original
five-second authoring docstring precedes later direction/retiming. Do not copy
old prose into a new timeline without tracing the final data and sampling path.

The current source and validation candidate match byte-for-byte across145
reference-named Paete/Phaister/Voodoo/intro files checked before new capture.
Fresh native reference captures are now complete for both heroes. Source reads
and sampled frames still do not constitute normal-speed human film approval.

## What must improve in our current Hydro work

Mechanics are ahead of presentation. Crosscurrent has checked interception and
Skim has checked ground travel, but reused feint/cut clips and icons are not final
acting. The new curtain is readable but looks like a cyan-edged glass pane. That
is a documented failure of material identity against the reference bar.

Acceptance for the presentation pass is specific:
- An unprompted viewer can distinguish scoop/redirect, sole-coating, curtain-lift
  and flood-release from body silhouettes and timings, without reading labels.
- Hands physically act on the held slipper or grounded water source; no unrelated
  hand flourish, floating prop or pose/prop release mismatch.
- Water develops a fold/lip and drains through its own form, with a clear centre
  preserving the can, slippers and threats. More particles cannot hide bad shape.
- Important contact has anticipation and a readable response; quiet recovery lets
  the next decision read. Effects are reviewed in normal-speed court playback.
- First-person, observer and affected-player views each communicate the same
  accepted event, including missed/refused/interrupted cases.
- Comparison records concrete wins and remaining weaknesses. No invented quality
  scores or superiority claim based on subscription price or green tests.

## Audio diagnosis and replacement gate

The legacy generators synthesize deterministic mono44.1kHz/16-bit sounds with
NumPy. Paete uses pulse/resonant creaks, impulses, modal knocks, granular rustle
and Karplus-Strong plucks; Phaister borrows filters but changes motifs to flutter,
cloth/pin ticks and glass/music-box tones. The writer normalizes each cue toward
an assigned peak. That guarantees neither a pleasant timbre nor useful relative
loudness. Similar peaks can flatten hierarchy; resonant/pitched layers may become
fatiguing in repetition. These are source-based risks, not a listening verdict.

Current AudioCues.SkillSfxOn is false and the registry documents deleted skill
cues. Re-enabling that flag alone is not a sound rework. Replacement must be
selective, registered, timed to actual beats and heard against core game sounds,
including overlaps and repeated use. The cloud currently reports no ALSA output
device; investigate the actual audio route before claiming listening validation.
Human voice assets remain outside a synthetic SFX replacement.

## Additional primary audio research

Blizzard's [sound-design breakdown](https://news.blizzard.com/en-us/article/24262573/weekly-recall-let-s-break-it-down)
explains layering synthetic material with recorded physical sources and giving
allies/enemies distinct, restrained targeting signals. Its examples also retain
an established fictional technology's sonic identity rather than replacing every
sound indiscriminately. TUMP takeaway: Rafi's caster detail can be intimate while
his opponent warning stays distinct, and real water/material texture can ground
controlled synthesis. This is a read article, not newly watched/heard footage.

The [Echo audio article](https://news.blizzard.com/en-us/article/23411614/from-zero-hour-to-hero-inside-echos-audio)
connects source selection to the actual model and deliberately treats first-person
and enemy presence differently. TUMP takeaway: repeated self-cues must tolerate
constant use, while rival cues communicate a real threat at a useful distance.
Do not copy a sci-fi palette into the street game or copy its voice treatment.

[ALSA's official plugin reference](https://alsa-project.org/alsa-doc/alsa-lib/pcm_plugins.html)
describes a null sink and file capture. These may help test a cloud recording
route, but a null sink produces no audible speaker output and cannot establish
listening quality. No global audio configuration has been changed. Official Unity
mixer page fetch failed; it is not counted as read documentation.

## Fresh Phaister time-sample review

The existing native18second/three-view probe completed540frames per view and its
single case passed, with no source drift or new OOM. The reference assets match
source; this is a fresh cloud OpenGL capture, not borrowed proof from an old film.
Reviewed owner samples at frames30/90/150/170/185/205 and court/doll samples300/420.
These are explicit time-sample observations; normal-speed human/audio approval
is not inferred from individual frames or the clock test.

What works visibly: the early caster pose is restrained against the changed sky;
the huge eye becomes the single subject; the close shot withholds the doll's face
before revealing its tilted stare; then almost everything is removed except the
eyes in black. This is contrast and controlled attention, not constant maximum
particle density. The camera and prop each have a specific job in the reveal.

What requires care when transferring the method: the first handback sample has
the companion filling much of the first-person view, and the wide review camera
sometimes frames through foreground tricycles. Neither still alone proves a
product defect; they do show why a beauty shot cannot replace the real handoff
or an unobstructed comparison view. Fine dark lines on the live doll also need
platform/material context before being labelled authored detail or rendering
error. Do not redesign the protected reference from these observations.

For Hydro, retain the lesson of holding one readable event and clearing the
frame for the next decision. Do not borrow the eye/iris/puppet staging or replace
water identity with purple spectacle. The current plain curtain still fails
material identity even though its collision and transparency checks pass.

## Fresh Paete time-sample review

The existing probe passed its one case, with540frames each for owner, court and
caught-player views. Frozen input hashes did not drift; OOM counters stayed
unchanged. The round clock held at89.580through the scene and read88.580one
second after handback. [Native receipts](reference-checks/) cover these narrow
claims, not overall multiplayer or player-build qualification.

Reviewed owner frames30/90/180/210, victim270and wide360. The early joined
character pose conveys companionship before the environment takes over. The
late tree shot explains the restraint with visibly connected branches and bound
players; after handback that same structure persists in the court. This physical
continuity is stronger than a disconnected cinematic followed by a generic ring.

The frame90subject sits far left, leaving much empty court; judge the moving
transition before calling it a framing failure. Close late shots crowd several
faces and arms, while the wide court separates bound players more clearly.
These observations motivate view-specific review for Hydro, not changes to the
protected reference. A water curtain must show where the material rises and how
it catches the shoe, then visibly lose the same material when spent.

Both exports contain54seconds across three views. Phaister's cue log is empty;
Paete logs a theme whose WAV is absent. The exporter reports that missing cue.
These exports therefore provide visual reference only. They must never be
presented as actual audible game-output recordings or SFX listening evidence.

## Audio-route check before replacement

A process-local ALSA null configuration opens successfully where the default
PCM reports no device. No global or user audio setting changed. This only
proves a sink can open; Unity output and usable capture remain unproven. Reuse
the existing ReviewAudioCapture listener route before inventing another audio
recording framework. It explicitly fails if no engine samples arrive.

The subsequent [native output check](audio-route-checks/README.md) now produces
nonzero actual listener samples using clocked AudioRenderer mode. This resolves
recording access; it does not yet approve any replacement sound or hardware mix.
