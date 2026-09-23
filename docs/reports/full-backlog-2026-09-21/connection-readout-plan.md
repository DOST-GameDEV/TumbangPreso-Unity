# Task140.4: missing connection feedback

Status: research draft NOT ADOPTED,2026-09-23. No code was implemented from it.
The owner's supersession clarification required rechecking the old proposal against
current VISUAL-1. That adopted plan avoids extra textual states in ordinary play
and uses rings for timers; the completed HUD scope also remains binding. The
textual elapsed-silence proposal below does not become work merely because old140.4
said OPEN. Preserve this investigation as reference, not an execution instruction.

The P6 review confirmed that only NetSession reads/writes LinkRttMs and Link;
no current UI consumes those measurements. The completed HUD art is preserved.

## Evidence and technical boundary

NetSession.SampleLink already samples both sides once per second, with the host's
worst peer and200/450ms poor/bad thresholds. Current native runs contain those
state transitions. This measurement is not itself a visible warning.

The installed primary NGO source was inspected in the owned qualification checkout:
com.unity.netcode.gameobjects@d43d28498f17. UnityTransport.GetCurrentRtt explicitly
accepts NGO client IDs and translates them before reading the transport, so no
speculative ID-mapping fix is needed. NetworkManager exposes
GetClientIdFromTransportId for observing transport events safely.

NetworkTransport.OnTransportEvent exposes delivered Data on the main thread.
NetworkTimeSystem.OnTickSyncTime sends a host time-sync message once per second,
using the NGO unscaled clock, including when gameplay is paused. This provides
client-side host-traffic observation without adding heartbeat messages, RPCs,
services or protocol changes. Transport keepalive internals and exact remaining
disconnect deadline are not public application state.

## Design decision

Show measured elapsed host silence, not a guessed countdown to disconnection.
The old draft proposed8minus-last-packet-age, but transport keepalives and app data
are different; that could promise a timeout the transport never performs. Preserve
the original draft as history and state the observable condition truthfully.
No connection rules, timeout durations, ready quorum or simulation clocks change.

## Unadopted implementation proposal, retained as history

1. NetSession passively observes its existing transport's Data events. Pair the
   subscription and cleanup with the existing session lifetime, initialize/reset
   observations on connect/stop, and translate transport IDs through the public
   NetworkManager API. Only client traffic from the host drives the host-silence
   warning. Do not treat a quiet player's movement or paused FixedUpdate as a loss.
2. Retain the existing RTT thresholds/sampling, cache per-peer readings for the
   host's pause sheet, and resolve peer names/seat identities through the existing
   lobby roster. Unknown/no-peer state is not a bad link.
3. Add a small non-interactive warning to the existing HUD canvas using the current
   primitives and black outline. Hidden for good/unknown/offline. Poor/bad uses a
   clear connection glyph/state; after4seconds without delivered host traffic,
   show a waiting state and measured elapsed seconds. Locate it using the actual
   current HUD bounds; do not overlay the aim area, existing score or touch controls.
4. Add the host's per-peer connection information to the existing pause surface,
   with useful names/seat labels and current sampled RTT/state. No settings toggle
   or extra permanent dashboard; the existing pause door owns the detail.
5. Check source/lifetime/reset semantics, actual bad-link/healthy/unknown rendering,
   a paused-but-connected case and restoration after fresh traffic. Reuse existing
   UI capture and network-shaping tools for the changed behavior. One bounded
   native outage/latency follow-up on the final integrated candidate, not a new
   diagnostic framework or repeated full-game sweeps.

Any future connection presentation needs a current user-facing requirement and a
design consistent with the adopted signal language. This draft authorizes no work.
