# Airburst acceptance criteria

Each line names the evidence that settles it. Assistant review, native tests,
actual peers and human approval are separate levels and are reported separately.

## Timing honesty

- [ ] The shipped baked clip's release key (arms at full forward drive) is at
  `AmihanRules.StormSurgeGatherSeconds` within one 60 Hz frame (EditMode guard on
  the asset that `person_amihan.asset` references).
- [ ] In a native film the gameplay release frame (first victim carry) and the
  body's forward drive coincide within two film frames.
- [ ] First-person hands reach their drive on the same frame band.
- [ ] The cutscene contains no release: no front leaves her, no victim reacts.
- [ ] The handback frame and the first live frame show the same pose and fan.

## Readability at normal distance and speed

- [ ] Effects off: the coil, two pack beats, draw back, push and recovery read
  from the court and from an observer at normal speed.
- [ ] Effects on: from a player standing outside the fan, both edges, the beat
  steps and the release are readable; the can, chalk and slippers stay visible
  through the interior at the strongest frame.
- [ ] Low graphics profile plus reduced effects keeps edges, chevrons and the
  release; only cosmetic motes may thin.
- [ ] No decorative element extends wider than the 30 degree contact edge.
- [ ] No full-screen white flash; no frame blown out by the release.

## Cutscene

- [ ] Exactly 3.6 s; round clock frozen across it (film clock assertion).
- [ ] Three shots on one side of the action; no camera inside a body or wall
  (shot report recorded).
- [ ] The final shot shows the lane direction on the real court.
- [ ] No vortex lift; she stays grounded.

## Cleanup and interruption

- [ ] Round end during the windup: no storm or fan objects remain within a
  frame, no release happens, and the body leaves the coil.
- [ ] After a normal release nothing from the fan remains at gather + 1.0 s.
- [ ] Inside/outside/caster outcomes match the existing Airburst rules
  (victim launched and Whirled, outsider untouched, caster unaffected).

## Scope

- [ ] No changes to mechanics, protocol, audio or other heroes; only owned paths
  in the commit.
