# Water wall implementation plan

The published defending Hydro role is a stationary four-metre-wide thin curtain,
lasting four seconds and placed within four metres. People cross freely. The first
flying slipper crossing drops on its approach side; the curtain breaks. It never
resets the can, follows the defender or blocks every throw for its full lifetime.
The cooldown is 35 seconds. No held/loose shoe is intercepted.

## Authority and geometry

Reuse the existing held-aim/accepted-cast route and rafi_skill2d identity. Validate
the entire width against playable bounds, nearby floor heights and solid cover
before spending resources. The footprint remains a single line, not an enclosure.
Append a Waterwall world kind and reuse existing Rafi reliable state/replay routes.
A host sweep compares previous/current slipper positions with the finite curtain
plane and chooses earliest crossing. Resolve one real slipper only, preserve its
owner, and end its existing flight on the incoming side through a bounded host
landing path. Body movement never depends on a curtain collider.

The snapshot stores placement, orientation, lifetime and whether/when it broke.
Restoration cannot intercept on clients or replay a hit. Match/round/expiry clear
the object through the existing field lifecycle. New semantics need compatible
clients. No additional transport or independent scoring system.

## Presentation

Use a distinct near-clear vertical sheet, a shaped upper lip and sparse side
edges, rising from the street into the hands' upward motion. The can and players
stay visible through its centre. After interception, the top edge folds inward
and drains to the real floor over a short sampled retirement. No opaque blue box,
solid collision wall or generic travelling current reused as the final picture.
First native view checks formation, active and broken states on the actual court;
full body/FPP/sound direction follows the same hero presentation pass as Skim.

## Checks

Placement success/refusal with no resource loss, first crossing only, correct
approach side/owner/loose result, no human collider, timeout/round cleanup, scoped
world recovery and render-only replay. Capture native active/broken views and
critique transparency/edge clarity. Matching actual peers remain a separate gate.
