# Throw Landing Circle, 2026-09-30

The owner's newest Feedback row replaces the directional flight line with a circle
on the predicted ground contact. Only the circle is drawn. It follows current
launch velocity, signed curve, world banks and supporting floor. Local camera,
charging and release gates remain. Refresh is bounded at20Hz while charging.
Players, can contacts and later ability/map recovery can still intercept a throw;
this is a pre-interception landing preview, not a promised final resting place.

Real flight and preview share the existing gravity/curve/terminal-speed calculation.
Physics behavior is retained. The supporting-floor query now reuses64 hit slots;
if full, it uses the original complete query so nearest/highest results cannot be
lost on dense geometry. Actor/slipper/can exclusions are retained. No loading,
kit, model, map, lighting, animation, sound or effect asset was edited.

## Native Evidence

Four unchanged native cases pass: actual host flight agrees with the circle within
0.12m for flat, raised/curved and wall-bank shots;100 warmed floor queries allocate
zero managed bytes. A70-collider fixture verifies the highest surface survives
the overflow path. These are local functional/allocation checks, not player FPS.

The initial visual fixture falsely appeared sufficient because it called Rebuild
directly. Later actual render-frame checks caught a camera facing away and an
illegal charge inside the defender box. A diagnostic also dereferenced a missing
bot component on the human seat. Those fixture mistakes cost additional focused
runs; no runtime assertions were weakened or unchanged physics cases repeated.

The final legal-charge case explicitly asserts throw eligibility outside the box,
uses the real countdown and charged carrier, keeps the circle visible across actual
render frames and hides it on release. It passes1/1 in8.9177248s. The on/off render
shows106 changed pixels at1280x720; the final1920x1080 gameplay capture was inspected.
Runtime input hashes stayed unchanged through the visual corrections. Physical
devices, actual peers and every-map/hero-interception acceptance remain separate.

- [Initial four physics/support cases and inadequate visual fixture](checks/landing.xml)
- [Actual-frame failure](checks/landing-visible-final.xml)
- [Legality diagnosis](checks/landing-state.xml)
- [Final legal charge/render/release](checks/landing-legal-final.xml)
- [Frozen inputs](checks/landing-inputs.json)
- [Inspected gameplay capture](Landing-circle-charge.png)

Charge-feedback clarity remains open in F0930-18. The shorter1.25s charge was
already shipped independently; it was not retuned again here.
