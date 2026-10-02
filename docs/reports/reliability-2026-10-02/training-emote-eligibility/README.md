# Emote eligibility respects the training verb restriction

GuidedTraining.ApplyVerbLock withholds Verb.EmoteWheel until its own lesson.
The actual EmoteWheel reads the input action directly, and MatchInstaller sends
its selected emote to EmotePlayer.Request. Request and HostPlay both use CanEmote,
which previously checked the actor's playable state and existing emote but ignored
the lesson restriction. Its public eligibility gate therefore accepted a verb the
current lesson had withheld.

CanEmote now also rejects Intent.Locked(Verb.EmoteWheel). The change uses the
existing shared verb contract and does not alter bindings, lesson progression,
animation, camera behavior or hero kits.

## Native evidence

Three focused PlayMode cases use real CharacterMotor, EmotePlayer and InputIntent
objects. They change the same AllowOnly restriction that the guided route owns and
assert the public CanEmote contract used by the existing request callers.

- Original run 6196: exactly 3 cases, 2 controls passed and 1 intended causal
  failure. A withheld emote verb still returned true for eligibility.
- Candidate run 8973: exactly 3 cases passed. Withheld emotes are refused; explicit
  lesson unlock and ordinary unrestricted eligibility remain available.
- The native fixture and valid 32-hex metadata were identical in both runs.
  Zero fixture repairs. One bounded tooling correction changed the job category
  from CPU to the exclusive GPU classification required for PlayMode. The initial
  scheduler preflight was rejected before Unity or profile preservation started.
- Each preparation completed with exit 0 before its dependent native launch.
  Exact 3 owned inputs match MAIN/qualification; all 12,529 protected qualification
  hashes remained unchanged.
- Unity 6000.5.8f1, batch/nographics PlayMode, profile training-emote-eligibility1002,
  2048 MB job with 2048 MB reserve and a 450-second ceiling. Both guards are terminal,
  preservation completed and no lease remains. No browser or preview was opened.

Raw baseline/candidate XML and job receipts, owned-input manifests, case counts,
the preflight correction and protected-input result accompany this report. Full
native logs and protected hash snapshots remain in qualification
Logs/training-emote-eligibility1002; the original source remains in MAIN at the
same Logs path.

This qualifies the public eligibility boundary only. It does not exercise Request,
render an emote clip, select the wheel on hardware, qualify transport, build a
player or complete the full tutorial.
