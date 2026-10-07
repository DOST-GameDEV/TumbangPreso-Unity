# Charged pursuit retains actual consumer age

The natural Arena gate trace in the PC map-clock report shows planner hold age0.057998 remaining unchanged through Cover while the normal consumer continues charging. On returning to Hunt, the producer timer could be younger than the charge it was controlling and delay a ready aimed release.

Original native49736 reproduces the mismatch with producer age0.05 and actual consumer charge0.45 or1.0. Three cases fail. The underpowered4.4m case correctly holds its first attempt but still delays the next one after the consumer reaches full power; the reachable3.5m partial and4.4m full cases also fail to release. Full original receipts are retained.

The producer now synchronizes its held age from ObservedLungeCharge before advancing its frame timer. Physical travel prediction still uses the consumer's already accumulated LungeChargeRatio, so the producer's frame time cannot invent extra power. Existing facing, edge, obstruction and reach checks remain. Targetless plans preserve the held button and the normal can-reset channel still cancels through the consumer. No direct dash/cancellation, gameplay retune or authored hero/old-map presentation change is added.

Candidate50152 passes24 native continuity, real motor-turn, tag commitment, power, obstacle/capsule/kerb and hold-lifetime controls. These staged scenarios use the normal input/consumer path and20/50ms producer steps; they are not a whole natural match or an improved hit-rate claim. All21369 frozen inputs and preferences restore for both original and candidate after the native and preservation parent terminate. Natural all-map engagement and complete feature/role/performance acceptance remain open.

The recorded shader-clock fix is already separately published and validated on the PC. Desktop725e remains its previously qualified release pending current-source package checks. Laptop effects/Archive/floor work stays reserved. No LAN, new worker/service or foreground PC control was used.
