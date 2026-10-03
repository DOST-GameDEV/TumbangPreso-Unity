# Opening handoff and nameplate correction

Owner playtest on October3 reports a player-view flash followed by an abrupt
jump to the sky, and asks for overhead names to stay hidden during introductions.
The supplied screenshot also shows the3 countdown below the arena.

## Source causes and repair

The arrival previously waited for HubLoading.Visible to become false. Loading
already spent0.3s fading its opaque canvas while that flag remained true, so it
revealed the gameplay camera before the opening could own it. The replacement
waits only for preparation to complete, prepares its camera under the curtain,
and starts its own0.5s reveal after loading finishes. Direct arena entry also
gets an opaque arrival curtain before its first yield.

The return transform was copied from the current camera while the world clock
was held. The rig skips LateUpdate during that hold, so copying an inherited or
prewarm-restored transform cannot establish a valid gameplay eye. The rig now
resolves its existing FPP/TPP placement explicitly before the arrival saves it.
This reuses its body-yaw, eye-height and lens logic rather than a second formula.

CharacterNameplate suppresses its label while MatchArrivalPresentation owns the
introduction. Cancellation clears that ownership so ordinary nameplate rules
resume. Floor rings, gameplay labels after the opening,3/2/1/GO!, protocol143,
custom map choices and other contributors' near-wall/motion-setting work remain
unchanged.

## Verification

Four native regressions passed: opacity before the first yield and cleanup,
opening camera already prepared while loading is still visible, player-eye
recovery from an inherited under-map camera while held, and label hide/restore.
Full-scene normal-speed and live-peer/player acceptance remain separate.

Final combined PlayMode acceptance:7/7 passed in4.1330504s at16:18:40–44UTC,
exit0, no guard stop. This includes the four handoff cases plus three countdown
controls. Peak process tree4535787520bytes, container7342993408bytes. Six frozen
changed inputs match both source and native checkout; both isolated project
settings restored exactly. Initial compile run passed4/4 but hit the memory
guard during shutdown; its evidence is retained and not labelled guard-free.
