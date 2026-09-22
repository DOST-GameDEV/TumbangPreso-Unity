# VISUAL-1 batchB: lighting, court and hero objects

Five maps now use an authorable world-look profile with lower tinted ambient,
per-map two-band Toon ramp, upper away-key rim, body foot gradient, label-safe can
cap highlight and horizon fog. Preview cameras retain their own shader context.
Original scene lighting is cached/restored at off; existing maps were not rebuilt.
The existing WorldOutline depth/normal/exclusion pass supplies subtle lower-wall
contact instead of a per-building geometry pass. Soft contacts follow actual
players/can/loose shoes and recorded poses; no new physics.

Authored court marks receive reversible medium overrides: charcoal on light paving,
chalk on darker surfaces, static grain and a scuffed home area. Supported court
overlays use different wear for pavers, asphalt, damp concrete, roof tile and deck.
They modulate the lit floor, preserving real shadows. Imported meshes, materials,
colliders and routes are unchanged. WorldCueProfile exposes independent off levers.

The can's permanent red rim yields to object/cap/ground contrast. A brief dotted
footprint follows actual toppling/settling or an elevated received pose. Normal
knocks stay in contact with the floor, so there is no invented ballistic trajectory.
Ordinary slipper strokes use actual ThrowerSlot identity, shorter tails and two-tone
ink, with the same ordinary shader in replay. Affinity effects stay independent.

## Evidence and limits

- v2-world.xml: retained actual can settle/elevated-pose/recorded-support pass.
- v5-world.xml: ordinary stroke1/1,6.812s,guard84b80b438847. Real guided-training
  spare/thrower,8bit identity colour, ink shader, recording/replay, off and re-equip.
- v7-world.xml: map/lighting2/2,42.088s,guard6cbcc32a18be. Five maps, before/after/
  comfort, original-settings restoration and nested world/portrait shader scopes.
- lighting-response.csv: raw HDR uniform-Toon diagnostic1.9472:1 original,
  2.3885:1 selected, actual global weight0/1. Profile shadow.44 was saved through
  Unity's authoring API (guardd8dec558d970). This is linear lighting response, not
  a universal display/hero-colour ratio. Diagnostic cube was removed, never an asset.

All five final normal and25percent-grey comparisons were personally inspected,
with reduced effects, high contrast and HUD120 at960x540. Can/stroke images were
also inspected; their earlier lighting version demonstrates state/shape, while
v7 is the final map look. Keep look3. No native build, full regression, performance
claim, real peer latency qualification or human interpretation approval. Those stay P7.

## Preserved failures

v1/v2 settled marker mistook imported renderer bounds for flight; the actual tilt
support law fixes it. Bridge chalk needed its explicit parent/node identity.
The first stroke fixture attempted a forbidden other-owner throw; ownerless spares
are permitted only in training, which the corrected fixture states explicitly.
Trail colour is8bit; expected encoding was corrected, not assertion tolerance widened.
The first wear overlay washed out shadows; lighting/surface isolation pictures
identified it, and multiplicative modulation fixes it. Shadow.37 was measured at
2.7978:1 and softened to.44; the final assertion was strengthened to2..2.5.
All XML and ledger history are preserved. Stop repeating passed risks.
