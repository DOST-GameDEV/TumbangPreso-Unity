# Actual two-machine loss during an active round

Both machines used the previously verified identical protocol145 package,
source8f7f304b543d93db71ed09f9fbaa4107fc365895/logicalad0026. Newer
presentation source is not requalified by this older package.

The first attempt used the existing completed-arrival observer. Its own
110-second Finish path calls Application.Quit(1), and it exited before LIVE.
That failed coordination/diagnostic attempt is retained separately; it is not
an active-round recovery result or a game defect.

The corrected test armed the laptop's fixed client command before starting
the host. A task-owned control endpoint started the client immediately once
the PC was hosting and returned actual player log/PID state. No chat/model
response was needed during the short active round. No completion observer,
scheduler, RAM cutoff or external process timeout was used.

PC host11380 and laptop client2440 both entered actual round1 in Eskinita.
The host log records two human admissions and normal readiness. The captured
client state sawLive/stillLive is true and has zero match-end events. The PC
driver then stopped only its retained host Popen handle. Host terminal data,
profile/input restoration and full artifact stability all passed.

The laptop subsequently observed HostLost before a match-over event. Its
actual final NetState report is MatchSetup/round0/activeFalse/protocol145,
normal exit0. No career file exists: history/queue/witness are explicitly zero,
so no completed result was fabricated. Input/profile restoration is reported;
exact client raw publication remains the combined evidence gate.

The endpoint's historical sceneEskinita comes from its live log and is not
the final retirement scene; the actual final state report owns that fact.
Log/PID evidence proves the bounded active loss and terminal retirement.
Direct clock/input-epoch/body-state inspection, physical operator input,
retained-seat recovery, Relay/WAN and newer-source acceptance remain separate.
Do not turn the host driver's passed flag into a whole-game recovery claim.
