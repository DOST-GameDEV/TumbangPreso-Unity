# Distinguish requested shutdown from transport failure

Current owner QA remains OPEN: an online join request times out while the host
reportedly returns Home after somebody joins. The screenshots show a generic
transport shutdown envelope, which does not identify who initiated it.

NetSession now records an application-requested stop with the initiating managed
call stack only while the transport is listening. Server-stopped and transport-
failure callbacks record whether an application stop preceded the event. The
flag resets before each new start; hooks are removed on replacement/destruction.
No account identity, credential, address or room code is logged by these events.
No movement hot-path logging, new server or gameplay behavior is introduced.

Native PlayMode actual listen-host start/Stop/restart/external-manager-Shutdown
passes the focused case. The two real shutdowns are distinguished True/False
and resetting the flag is demonstrated by the second fresh session. Parent20576
exits0, no frozen source changes and profile/shared input/preferences/Quality restore.
The first fixture improperly treated ordinary informational approval logs as
unexpected; that extra assertion was removed while keeping exact lifecycle
expectations. Its failure remains private. A subsequent launcher attempt reused
a profile path and did not start Unity; the final isolated launch is the receipt.

This is actionable shutdown-origin evidence, not a reproduction or fix of the
owner's unexplained host loss. The next exact Windows package must exercise
normal online host/public join/idle/exit controls and retain both logs.
