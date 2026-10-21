# VISUAL-1.7 first-person framing and throw

The hands and held slipper keep a fixed95degree apparent FOV while the world lens
still spans75..110. A camera-plane parent compensates the whole assembly, including
ink and attached effects, without another camera/RT or double grading. Pooled
camera scopes restore transform, visibility, shadows and material blocks. The
spectator POV uses the same ViewmodelArms path; unrelated cameras hide these copies.

Rest scale drops from.72 to.64 and the seat lowers.08m. Existing lit Toon arms and
authored colours remain; the held slipper gets a separate small rim. Private arms
do not cast another world shadow. No imported character, rig, model, palette or
builder asset changed. WorldCueProfile.ViewmodelFraming0 restores previous framing.

A.12s wrist set occurs inside the existing charge. The release uses the existing
.055s contact,.18s follow-through and.54s settle, now with a lower-screen pivot
sweep past centre and a little clearance from the other hand. Each previous offset
is removed before posing, so nothing accumulates. Reduced motion/off removes the
extra movement. Real aim, muzzle origin, charge rules and release timing are intact.

## Evidence

- v1-world.xml:2/2,14.780s,guardc57adf5ed442. Held viewport bounds invariant across
  actual75/95/110 world FOV; physical origin invariant; both nested camera orders;
  off/pose/block/shadow restoration; lit/rim checks; real charge preparation,
  immediate accepted release and protected refusal retaining the shoe.
- v5-world.xml:1/1,9.771s,guard1583cce5c39f. Real sightline release,34post-pose
  frames at60Hz. First projectileviewport(.5005,.4999); hand crosses lower centre
  to x.4368, stays in the useful view and settles its transient offset exactly0.
- Rest/charge/FOV/comfort960x540 frames and25percent-grey sheet personally inspected.
  release-before/ is v4 before the new sweep; release-after/ is v5 with it. Full
  keyframes and16-frame-grey sheets personally inspected. GIF durations alternate
  centiseconds to represent60Hz average, rather than rounding every frame to10ms.

Keep rest look1 and release look2. These are controlled gameplay captures with a
real throw, not native free-play/human approval. Integrated native, spectator and
all-cast review remain P7. No native build or whole-suite repetition.

## Retained failures and scope

v2's new CSV witness missed its System namespace and did not compile. v3 proved
the physical sightline but rendered in a coroutine before LateUpdate, so its first
picture showed the prior pose. That was a capture error, not a runtime held-visibility
lag. v4 moves photography after the camera/arm writers and revealed the actual weak
follow-through; v5 supplies and checks the sweep. Failed compile log remains in
Logs/look-1.7-v2. v3/v4 XML and ledger history are retained here. No weakened tests.

Final static scope review gates the new preparation pulse on actual carrying, so the existing empty-hand lunge charge is unchanged. The real held-charge cases above cover the active branch; this small exclusion follows the last focused run and enters the next compilation/P7.
