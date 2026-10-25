# Electrical gameplay effects

Start from the owner's audible complaint. On 2026-10-25 Zack's short cracks and rising tones did not communicate enough buzz or taser. The revision needs a sustained rough electrical body, rapid arcing and a clear contact burst. A louder initial snap alone does not solve that problem.

Build the electrical body before the transient. Useful ingredients are a pulse train with a changing duty cycle, a rough modulated carrier and filtered noise excited in irregular short arcs. Keep substantial midrange energy so it survives small speakers. Do not rely solely on sub-bass or high hiss. Oversample nonlinear stages, then filter and downsample to avoid brittle aliasing.

Give related actions different behavior: a rising pull that catches on arrival, a double split with a moving bed, a taut wire with stationary current, a rough contact rasp and an empowered body with a quieter persistent crackle. Preserve their common material without recycling one complete waveform. Tune the body against the initial crack; peak normalization can leave a loud transient and an inaudible sustain. Inspect the sustain separately and compare at matched playback level.

For loops, check the actual wrap, remove DC and keep the bed below casts and voices. Tie loop lifetime to the replicated live effect. Stop at pop, snap, expiry, disable or removal. Pause with the game's presentation hold and obey SFX settings and the per-cue allowlist. Do not enable the global skill sound switch to release one hero. Rendering replay visuals alone must not create live emitters.

Use original synthesized sources under current TUMP policy. `tools/sfx_electric.py` provides the electrical primitives; `tools/build_zack_skill_sfx.py` is one authored kit example, not a universal sound recipe. Preserve existing asset GUIDs when revising cues. Register shipped clips in AudioCues.Live and the scoped allowlist, retain bus headroom and apply per-cue trim exactly once. Loop sources must also follow current SFX gain.

Capture actual game audio for final gameplay proof. ZackSkillFilmProbe uses Unity AudioRenderer with Time.captureFramerate, calls Render every frame using the exact sample count times the output channel count and always stops recording on exit. This captures the real mix including distance, loop sources and the introduction. An offline mix made from events is an audition, not evidence of native playback. Check non-silence, clipping, duration and cue traces, then review the synced film. Metrics cannot establish whether the sound feels good.
