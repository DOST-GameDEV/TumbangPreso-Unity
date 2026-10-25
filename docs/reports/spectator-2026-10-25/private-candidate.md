# Private player-view candidate

This candidate is not yet complete or qualified for main integration. Main's
[perception unit](perception.md) is separate.

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
The initial v2 player passed raw private delivery, rendering and cleanup through
three real processes. A repeat exposed a fixture shutdown race, which was fixed
with ordered completion. Visual inspection then found the source reticle hiding
defect above. Final player build and owner/observer visual comparison remain
pending for that correction; no natural/WAN/device claim follows.
