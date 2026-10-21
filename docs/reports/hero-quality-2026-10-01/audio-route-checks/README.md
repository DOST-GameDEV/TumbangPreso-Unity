# Native game-output capture route

One opt-in native case now passes, producing actual listener output from the
existing score-award cue in the loaded game. No microphone, synthetic replacement
or external soundtrack. Capture:48kHz stereo,1.493333seconds,143360interleaved
samples, peak0.058779and RMS0.003304. Frozen source hashes match; no new OOM.

Default ALSA has no device. A process-local null sink opens and removes that
initialization failure, but runs without real-device pacing. The first listener
capture therefore correctly failed its bounded buffer. The one tooling repair
uses Unity AudioRenderer recording mode at30capture frames/sec for45frames;
the existing ReviewAudioCapture writes the actual samples. Mode/settings restore
in finally. No global audio configuration or production mixer changed.

Earlier old112player attempts never reached the expected menu before timeout,
so they are not audio passes. The successful check is this current Editor route,
not a new player, hardware speaker test or subjective sound-quality approval.
The cue is a route witness, not newly authored SFX. SkillSfxOn remains unchanged.

[Unity AudioRenderer.Start](https://docs.unity.com/en-us/engine/6000.5/script-reference/unityengine/audiorenderer/start)
provides the explicit recording mode. Direct Render/sample-count documentation
fetch timed out; signatures were validated by this native compile/run.
