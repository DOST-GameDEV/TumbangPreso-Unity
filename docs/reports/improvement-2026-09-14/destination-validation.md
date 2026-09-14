# Destination throwing, equipment and restoration checkpoint

This is a partial implementation checkpoint on ASTRAReworks after transfer986542f4.
The full project queue remains active. The owner explicitly deferred Inday FPP
framing; it is not marked accepted or complete.

## Source and rendering

Stable10slipper and6can IDs/order retained. Slipper rows now each spend9points
and no row dominates another. Recovery controls pickup lock and aim-settling time;
can rebound now controls the actual return velocity/lift, including protected hits.
The initial focused Core15/15 and Unity equipment probe passed. See equipment-destination.md.

Throw aim has bounded continuous angular drift: early holds1.45degrees, movement
2.4degrees per normalized unit, settled residual0.07degrees. Equipment modifies
settling speed without changing final residual or movement penalty. The guide and
reticle show nominal general direction while actual flight uses the current
perturbed direction. There is no exact landing marker or extra release-time roll.
A short0.24..0.74second path grows as aim settles, stops on real map surfaces, and
remains visible when an old AIController component is disabled. On/off rendering
in an active game after ReadyGate countdown measured562..672contributing pixels
at1280x720 across BayanPlaza, IlalimNgTulay and SaBubong. The3map source captures
are in Logs/aim-guide-active-v3. No visual claim about the deferred Inday arms in
those captures: they predate the owner's latest direct-copy request.

Carry-pose regression passed all18people x10slippers x6charge samples x3spin
samples using actual rigid head surfaces. Earlier AABB/hat bounds were false
positives. Maring's real intersection was removed by the wider throw preparation.
Nemu required a20degree outward yaw of her original holding-right clip; lowering
it by22or32degrees did not clear her hood. Actual quick, held-left and moving-right
Nemu/Alpombra footage then had zero penetrating vertices over6,82and78held samples.
Existing continuity and all-action/roster checks passed before the final narrow
pose correction. No full test suites were run on this PC.

Old Inday body geometry is restored from pinned backup4 without losing the current
33animations. Old small Nemu ghost is restored, with its original17boxes split
for expressive eyes/tail, and connected to the current monster's transition.
All11idle gestures and the6.945second mini/monster/mini sequence were captured at
ordinary timestamped speed. The current monster geometry, materials and rig digest
remains ef5a0d18520256b81d287bd191c6bda64f945e9c86c51b71b00fbd957fc44373.
Current Inday animation digest remains46c7921d3937b14cf6ebf9c8c714de9660e11d75ae729046bf5c799aa4b71050.

Zack's flat hand depth was increased while preserving his original motion and
outfit. Other FPP hands use the retained clean block frame and matching wardrobe.
Zack's band is left-only; Phaister's cuff stripe is gold; Nemu's lavender cuff has
no coplanar overlap. Inday now uses her actual source-arm triangles, but camera
framing is deferred in TODO.md by explicit owner instruction. Rejected red-mitten,
purple-band and partial-guard reconstructions are not final designs.

## Current player and network evidence

Fresh INTERNAL build: Builds/DestinationEquipmentReview/TumbangPreso.exe.
Build succeeded,1057MB,132seconds. No Desktop build/update.
Executable SHA256:6f44fe53090dad3edd9f86f5b5691b2cc8cba07deb4efd4791d7e32135385cb8.
Runtime DLL SHA256:c49a540168f800fcde948114e9d9f3b1fc208379be0d815091d1fbfbded82b17.
Only test/helper/report changes have followed that player build so far.

Classic actual host/owner/observer throw matrix passed straight and signed holds,
release and foreign-shoe warmup handover without stale re-equipping. Staged familiar
matrix passed, with matching field coordinates and yaw, zero model/body offset.
Delayed150ms-per-direction familiar owner reconnection passed in an isolated run:
24active restored-field samples, matching position, normal expiry and no refunded
charges. Running two3player matrices together first made the reconnect load after
the short ultimate expired; this was not proof of an effect reconstruction bug.
The helper also counted a post-host-shutdown lobby sample as a seat mismatch;
comparison now ends at the host's final match trace. Host logs showed actual seat1
reclamation in both runs. Saved profile bytes were verified after restoration.

Delayed Hero throw rerun passed with150ms delay in each direction and observer
rejoin. All31pre-disconnect negative and116positive observer samples matched,
with133rejoined active samples and no stale holding after release. The initial
failure was insufficient pre-disconnect coverage; the helper now waits for30actual
observer samples before intentional disconnect and retains its strict assertions.
See destination-evidence/network-hero-delayed-rejoin.json.

Helpers use distinct ports and named profiles. net_familiar_matrix.py was narrowed
from restoring the entire user profile tree to only famhost/famowner/famobserver.
No paid services, Figma, other conversations, agents or usage resets were used.

## Remaining next work

Review the directional movement baseline,
then continue movement/recovery, whole hero kits/alternatives, remaining network,
spectator and engineering queue. Complete deferred UI last. Inday FPP remains
in its explicitly deferred TODO, with her actual source copy preserved.
