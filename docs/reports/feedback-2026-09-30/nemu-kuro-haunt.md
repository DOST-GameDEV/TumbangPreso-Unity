# Nemu Kuro: Haunt chase and familiar delivery

## Behavior

The adopted Wiki replaces the old seven-second pulling seance with Kuro: Haunt,
costing15objective points. The host chooses a visible active roster player,
pursues that player, and applies Haunted7.5seconds only on contact. It then
selects another player. Each seat is contacted once per cast. The Wiki says
every player/everyone; this implementation includes Nemu herself, rather than
silently changing it to enemies only. Non-roster companion bodies are excluded.

Occlusion blocks acquisition and contact. A target that leaves sight is released
for another visible candidate. Unseen pending players keep the chase alive; it
does not expire at the retired seven-second deadline. Its bounded recovery clock
uses the remaining round, with180seconds only the maximum custom-round ceiling.
Everyone contacted, cancellation, reset or round exit releases the monster.
Kuro moves inside arena bounds through the existing swept/sliding collision path,
even when Nemu herself is the confined defender. It does not move Nemu.

The existing companion monster transformation, return, cast action and cue are
reused. No pull field is spawned. No new model, authored animation, VFX, SFX,
map, lighting or loading change is included. Missing companion availability
refuses before spending the15point meter.

## Delivery and performance

The existing scoped FamiliarEffect message now publishes moving positions at
most10times/second and a terminal zero clock. The host is still the sole outcome
resolver. Match, round, body epoch, hero, ability and accepted phase gates remain.
Within a phase, older/duplicate positive movement clocks cannot rewind the chase.
Completed lifetimes cannot restart; terminal recovery also clears a pending
windup so its delayed payload cannot recreate the monster. Recovery adopts
position/remaining time without replaying a contact, resource spend or cast cue.
Moving updates do not cancel newly started basic skills.

The repeated chase sweep uses a retained32hit buffer; saturated queries fail
closed at the last safe position. Visibility also uses retained storage.
This removes CapsuleCastAll array construction from the new per-frame chase
path. No measured whole-game allocation or frame-rate improvement is claimed.
Matching protocol113 builds are required; the packet shape is unchanged but
its live/terminal semantics and Nemu gameplay rules differ.

## Evidence and remaining work

Native D3D11 PlayMode against the existing real companion and private actual
FamiliarEffect receiver, from5cce9a552 plus explicit owned source overlays:

- Initial5/5: sequential contact/completion, occlusion beyond7seconds,
  observer/reset, defender reach, receiver movement/terminal/old-scope gates.
- Expanded receiver1/1: a terminal packet during windup clears delayed activation.
- Sweep2/2: real wall stop, saturated storage refusal, unobstructed travel,
  and defender reach using the reused-storage path.
- Final2/2: missing companion refuses without spending15points; the complete
  sequential contact/return case passes on the final reused-storage candidate.

Seven distinct cases pass across these receipts, not one final7/7suite.
No fixture repairs or failed native cases occurred. Final640input hashes have
no drift, and the eight owned runtime/test files match the native candidate.

Exact XML and frozen input manifests are retained in nemu-haunt-checks.
Each candidate is checked for input drift before the next intentional overlay.
The named-profile guard preserves existing profile files/shared input preferences.
Raw logs remain in the isolated checkout Logs/feedback-0930/nemu-haunt*.log.
No new whole-player, real-peer, reconnect, cross-platform, authored-map route,
monster film or human-feel qualification is claimed. The frozen protocol103
player does not qualify113. Nearsight and audio muffling remain unfinished;
F0930-12 and the original broad Feedback row remain open.
