# Private player-view candidate

The private presentation unit passed native checks and three actual Windows
processes. It extends the [perception unit](perception.md). Broader live-event,
network impairment and device coverage remain separate checks.

The owner's actual visible hallucination roots and held ground marker are sampled
with its camera view. The optional v2 codec permits at most nine phantom records
and one marker within a 111-686 byte pose. It validates complete data, identities,
count, type, finite coordinates, scales, rotations, styles and marker bounds.
The existing gameplay protocol and packets retain their meanings.

An observer copies only render meshes/materials from replicated bodies and shoes;
phantoms contain no actor, input, collider or rigidbody components. Root identity,
world position, rotation and scale come from the owner. Skinned silhouettes use
the replica's presented pose; this is not video or mesh streaming. Private copies
and the held marker clear on manual/free, stale, held-presentation, cutscene and
camera exits. The local owner retains its originals without a duplicate set.

Disoriented source geometry now hides when its owner's camera is no longer used,
switches victims correctly and releases its owned baked meshes on teardown.
The reticle copy preserves the sampled floor height and existing authored style.

Native checks so far:

- Private codec: 8/8, including maximum payload and complete truncation checks.
- Capture/render/lifecycle: 20/20 focused cases, including existing camera/body
  restoration. Earlier 12-case subset also passed.
- Six source/proxy checks include all nine render roots retaining world pose and
  scale beneath a transformed camera parent, real owner roots/marker capture,
  render-only components, source preservation and view-exit cleanup.
- Final source-visibility/menu/actual-view suite: 15/15. Original private geometry
  hides after a late camera release; an open menu cannot retain a stale stream.
  Marker beacons and world scale are preserved. [Native receipt](private-native-validation.json).
- Visibility/restoration follow-up: 29/29. An actual render capture exposed the
  player's FPP body-hide loop hiding its world reticle. A baseline regression
  reproduced it; both camera body-hide loops now preserve ground-marker geometry
  while still hiding the body. Own captions, overhead status, possession riders
  and Nemu's owner-only highlights/ghost opacity use the selected view identity.

The opt-in three-process tool now compares the actual sent owner packet to the
host by timestamp, then the host relay to the observer by scoped serial, including
the exact private bytes. `--private` applies host status clocks and draws an owner
marker through existing presentation paths; it checks all hallucination types,
observer rendering, unsubscribe cleanup and actual owner/observer captures.
The final run, `Logs/view-private-peer-1025-07`, matched 159 actual owner packets
at the host and 156 host relay serials at the observer, including exact private
bytes. All nine phantom roots, all three phantom types, held markers and beacons
arrived. The observer rendered nine roots and cleared them after unsubscribe;
owner publication stopped. Maximum observed eye error was 0.0165 m.

Owner and observer captures were inspected: the world marker now appears in both.
Render diagnostics identified an inactive-parent lookup in both body-hide loops:
they scanned inactive children but excluded inactive parents while finding their
GroundReticle owner. The inactive-start regression failed before the correction;
the fixed focused suite passed 28/28. A separate actual-map graphics test measured
60,748 changed pixels for both owned and ownerless reticles. The slipper tracking
follow-up passed 9/9, including ownership changes and object replacement.

The player source was `6ebd1b2e667b81688defd02cb693fd2870bca504`, gameplay
protocol 181; its runtime SHA-256 and test hashes are in
[the receipt](private-peer-validation.json). Main's subsequent Zack work changes
gameplay protocol independently; this cosmetic extension does not define it.
These are controlled same-PC LAN checks with a posed camera and explicit status/
marker fixture. They do not establish natural play, WAN, Relay or Android parity.

After merging main's current Zack work at `e843c300ca`, the focused integrated
PlayMode suite passed 49/49: private/actual views, body restoration/lifetime,
slipper identity and ultimate-event transitions. This does not extend the older
player build's network proof to the new gameplay protocol.
